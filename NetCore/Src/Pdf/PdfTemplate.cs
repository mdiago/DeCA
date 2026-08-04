/*
    This file is part of the DECa (R) project.
    Copyright (c) 2026 Irene Solutions SL
    Authors: Irene Solutions SL.

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License version 3
    as published by the Free Software Foundation with the addition of the
    following permission added to Section 15 as permitted in Section 7(a):
    FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
    IRENE SOLUTIONS SL. IRENE SOLUTIONS SL DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
    OF THIRD PARTY RIGHTS

    This program is distributed in the hope that it will be useful, but
    WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
    or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU Affero General Public License for more details.
    You should have received a copy of the GNU Affero General Public License
    along with this program; if not, see http://www.gnu.org/licenses or write to
    the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
    Boston, MA, 02110-1301 USA, or download the license from the following URL:
        http://www.irenesolutions.com/terms-of-use.pdf

    The interactive user interfaces in modified source and object code versions
    of this program must display Appropriate Legal Notices, as required under
    Section 5 of the GNU Affero General Public License.

    You can be released from the requirements of the license by purchasing
    a commercial license. Buying such a license is mandatory as soon as you
    develop commercial activities involving the DECa software without
    disclosing the source code of your own applications.
    These activities include: offering paid services to customers as an ASP,
    serving DECa XML data on the fly in a web application, shipping DECa
    with a closed source product.

    For more information, please contact Irene Solutions SL. at this
    address: info@irenesolutions.com
 */

using DeCA.Common;
using DeCA.Config;
using DeCA.Pdf.Appearance;
using DeCA.Pdf.Core;
using DeCA.Pdf.Forms;
using DeCA.Pdf.Images;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace DeCA.Pdf
{

    /// <summary>
    /// Representa una plantilla PDF basada en AcroForm.
    /// Permite abrir el documento, enumerar sus campos y modificar su estado en memoria.
    /// </summary>
    public sealed class PdfTemplate
    {

        #region Variables Privadas de Instancia

        /// <summary>
        /// Contenido binario original de la plantilla PDF.
        /// </summary>
        private readonly byte[] _source;

        /// <summary>
        /// Analizador utilizado para interpretar la estructura interna del documento PDF.
        /// </summary>
        private readonly PdfParser _parser;

        /// <summary>
        /// Colección de campos AcroForm encontrados en la plantilla.
        /// </summary>
        private readonly ReadOnlyCollection<PdfFormField> _fields;

        /// <summary>
        /// Índice de campos AcroForm por su nombre completo.
        /// </summary>
        private readonly Dictionary<string, PdfFormField> _fieldsByName;

        /// <summary>
        /// Imágenes pendientes de aplicar a campos de formulario de tipo botón.
        /// </summary>
        private readonly Dictionary<string, PdfBitmap> _buttonImages =
            new Dictionary<string, PdfBitmap>(StringComparer.Ordinal);

        /// <summary>
        /// Diccionario AcroForm asociado al documento.
        /// </summary>
        private PdfDictionary _acroForm;

        /// <summary>
        /// Referencia indirecta al diccionario AcroForm, si existe.
        /// </summary>
        private PdfReference _acroFormReference;


        /// <summary>
        /// Diccionario de información del documento PDF.
        /// </summary>
        private PdfDictionary _documentInformation;

        /// <summary>
        /// Referencia indirecta al diccionario de información del documento PDF.
        /// </summary>
        private PdfReference _documentInformationReference;

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa una plantilla PDF desde su contenido binario.
        /// </summary>
        /// <param name="source">Contenido binario de la plantilla.</param>
        internal PdfTemplate(byte[] source)
        {

            _source = source;
            _parser = new PdfParser(source);
            ReadDocumentInformation();

            List<PdfFormField> fields = ReadFields();
            _fields = new ReadOnlyCollection<PdfFormField>(fields);
            _fieldsByName = CreateFieldIndex(fields);

        }

        #endregion

        #region Métodos Privados Estáticos

        /// <summary>
        /// Crea un índice de campos mediante su nombre completo.
        /// </summary>
        /// <param name="fields">Campos que se desean indexar.</param>
        /// <returns>Diccionario que permite localizar los campos por nombre.</returns>
        private static Dictionary<string, PdfFormField> CreateFieldIndex(
            IEnumerable<PdfFormField> fields)
        {
            Dictionary<string, PdfFormField> result =
                new Dictionary<string, PdfFormField>(StringComparer.Ordinal);

            foreach (PdfFormField field in fields)
                result[field.Name] = field;

            return result;
        }

        /// <summary>
        /// Determina el tipo público correspondiente a un campo AcroForm.
        /// </summary>
        /// <param name="type">Nombre PDF del tipo de campo.</param>
        /// <param name="flags">Indicadores AcroForm del campo.</param>
        /// <returns>Tipo de campo reconocido por la librería.</returns>
        private static PdfFormFieldType GetFieldType(string type, int flags)
        {
            switch (type)
            {
                case "Tx": return PdfFormFieldType.Text;
                case "Ch": return PdfFormFieldType.Choice;
                case "Sig": return PdfFormFieldType.Signature;
                case "Btn":
                    if ((flags & 65536) != 0) return PdfFormFieldType.PushButton;
                    if ((flags & 32768) != 0) return PdfFormFieldType.RadioButton;
                    return PdfFormFieldType.CheckBox;
                default: return PdfFormFieldType.Unknown;
            }
        }

        /// <summary>
        /// Crea una fecha conforme al formato de fechas definido por PDF.
        /// </summary>
        /// <param name="value">Fecha que se desea representar.</param>
        /// <returns>Fecha en formato PDF.</returns>
        private static string CreatePdfDate(DateTimeOffset value)
        {
            string offset = value.ToString("zzz", CultureInfo.InvariantCulture)
                .Replace(":", "'");

            return value.ToString("'D:'yyyyMMddHHmmss", CultureInfo.InvariantCulture) +
                offset + "'";
        }

        #endregion

        #region Métodos Privados de Instancia

        /// <summary>
        /// Lee el diccionario de información definido en el trailer del documento.
        /// </summary>
        private void ReadDocumentInformation()
        {
            PdfObject informationObject = _parser.Trailer.Get("Info");
            _documentInformationReference = informationObject as PdfReference;
            _documentInformation = _parser.ResolveDictionary(informationObject);
        }

        /// <summary>
        /// Aplica los metadatos propios de los documentos generados por DeCA.
        /// </summary>
        /// <param name="writer">Escritor de la nueva revisión incremental.</param>
        private void ApplyDocumentInformation(PdfWriter writer)
        {
            if (_documentInformation == null)
                _documentInformation = new PdfDictionary();

            DateTimeOffset now = DateTimeOffset.Now;
            _documentInformation.Set("Title", PdfWriter.CreateUnicodeString("DeCA by Irene Solutions"));
            _documentInformation.Set("Author", PdfWriter.CreateUnicodeString("DeCA Irene Solutions"));
            _documentInformation.Set("Subject", PdfWriter.CreateUnicodeString("DeCA"));
            _documentInformation.Set("Producer", PdfWriter.CreateUnicodeString("DeCA by Irene Solutions"));

            if (_documentInformation.Get("CreationDate") == null)
                _documentInformation.Set("CreationDate",
                    PdfWriter.CreateString(CreatePdfDate(now)));

            _documentInformation.Set("ModDate",
                PdfWriter.CreateString(CreatePdfDate(now)));

            if (_documentInformationReference == null)
            {
                _documentInformationReference = writer.AddObject(_documentInformation);
                writer.SetTrailerValue("Info", _documentInformationReference);
                return;
            }

            writer.SetObject(_documentInformationReference, _documentInformation);
        }

        /// <summary>
        /// Lee los campos AcroForm definidos en la plantilla PDF.
        /// </summary>
        /// <returns>Lista de campos encontrados en el documento.</returns>
        private List<PdfFormField> ReadFields()
        {
            List<PdfFormField> result = new List<PdfFormField>();
            PdfDictionary trailer = _parser.Trailer;
            PdfDictionary catalog = _parser.ResolveDictionary(trailer.Get("Root"));
            if (catalog == null)
                throw new InvalidDataException("No se encontró el catálogo del PDF.");

            PdfObject acroFormObject = catalog.Get("AcroForm");
            _acroFormReference = acroFormObject as PdfReference;
            _acroForm = _parser.ResolveDictionary(acroFormObject);
            if (_acroForm == null)
                return result;

            PdfArray fields = _parser.ResolveArray(_acroForm.Get("Fields"));
            if (fields == null)
                return result;

            foreach (PdfObject item in fields.Items)
                ReadField(item, null, null, 0, result);

            return result;
        }

        /// <summary>
        /// Lee recursivamente un campo AcroForm y sus posibles descendientes.
        /// </summary>
        /// <param name="fieldObject">Objeto PDF que representa el campo actual.</param>
        /// <param name="parentName">Nombre completo del campo padre.</param>
        /// <param name="inheritedType">Tipo de campo heredado del elemento padre.</param>
        /// <param name="inheritedFlags">Indicadores heredados del campo padre.</param>
        /// <param name="result">Lista en la que se añadirán los campos encontrados.</param>
        private void ReadField(PdfObject fieldObject, string parentName, string inheritedType,
            int inheritedFlags, List<PdfFormField> result)
        {
            PdfReference reference = fieldObject as PdfReference;
            PdfDictionary field = _parser.ResolveDictionary(fieldObject);
            if (field == null)
                return;

            string partialName = _parser.ResolveText(field.Get("T"));
            string fullName = parentName;
            if (!string.IsNullOrEmpty(partialName))
                fullName = string.IsNullOrEmpty(parentName) ?
                    partialName : parentName + "." + partialName;

            string type = _parser.ResolveText(field.Get("FT")) ?? inheritedType;
            int flags = _parser.ResolveInteger(field.Get("Ff")) ?? inheritedFlags;
            PdfArray kids = _parser.ResolveArray(field.Get("Kids"));

            bool hasOwnName = !string.IsNullOrEmpty(partialName);
            bool terminal = kids == null || kids.Items.Count == 0 || IsWidgetOnlyChildren(kids);

            if (hasOwnName && terminal)
            {
                string value = _parser.ResolveText(field.Get("V"));
                result.Add(new PdfFormField(fullName, partialName,
                    GetFieldType(type, flags), flags, value, field, reference));
            }

            if (kids != null && !terminal)
            {
                foreach (PdfObject kid in kids.Items)
                    ReadField(kid, fullName, type, flags, result);
            }
        }

        /// <summary>
        /// Indica si todos los descendientes inmediatos son anotaciones Widget sin campos propios.
        /// </summary>
        /// <param name="kids">Descendientes que se desean comprobar.</param>
        /// <returns>Verdadero cuando todos los descendientes son únicamente widgets.</returns>
        private bool IsWidgetOnlyChildren(PdfArray kids)
        {
            foreach (PdfObject kidObject in kids.Items)
            {
                PdfDictionary kid = _parser.ResolveDictionary(kidObject);
                if (kid == null)
                    continue;

                string subtype = _parser.ResolveText(kid.Get("Subtype"));
                if (!string.Equals(subtype, "Widget", StringComparison.Ordinal))
                    return false;

                if (kid.Get("T") != null || kid.Get("Kids") != null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Comprueba que un campo puede recibir un valor textual.
        /// </summary>
        /// <param name="field">Campo que se desea modificar.</param>
        private void ValidateTextValue(PdfFormField field)
        {
            if (field.IsReadOnly)
                throw new InvalidOperationException(
                    $"El campo '{field.Name}' está marcado como de solo lectura.");

            if (field.FieldType != PdfFormFieldType.Text &&
                field.FieldType != PdfFormFieldType.Choice)
                throw new InvalidOperationException(
                    $"El campo '{field.Name}' no admite valores de texto.");
        }


        /// <summary>
        /// Comprueba que un campo puede recibir un valor lógico.
        /// </summary>
        /// <param name="field">Campo que se desea modificar.</param>
        private void ValidateBooleanValue(PdfFormField field)
        {
            if (field.IsReadOnly)
                throw new InvalidOperationException(
                    $"El campo '{field.Name}' está marcado como de solo lectura.");

            if (field.FieldType != PdfFormFieldType.CheckBox)
                throw new InvalidOperationException(
                    $"El campo '{field.Name}' no admite valores lógicos.");
        }

        /// <summary>
        /// Obtiene el nombre del estado activo definido en la apariencia normal
        /// de un campo de tipo casilla de verificación.
        /// </summary>
        /// <param name="field">Campo cuya apariencia se desea examinar.</param>
        /// <returns>Nombre PDF del estado activo.</returns>
        private string GetCheckBoxOnState(PdfFormField field)
        {
            PdfDictionary appearances = _parser.ResolveDictionary(field.Dictionary.Get("AP"));
            PdfDictionary normalAppearance = appearances == null ?
                null : _parser.ResolveDictionary(appearances.Get("N"));

            if (normalAppearance != null)
            {
                foreach (KeyValuePair<string, PdfObject> item in normalAppearance.Items)
                {
                    if (!string.Equals(item.Key, "Off", StringComparison.Ordinal))
                        return item.Key;
                }
            }

            return "Yes";
        }

        /// <summary>
        /// Crea y aplica las apariencias de imagen pendientes para los campos de botón.
        /// </summary>
        /// <param name="writer">Escritor de la nueva revisión incremental.</param>
        private void ApplyButtonImages(PdfWriter writer)
        {
            foreach (KeyValuePair<string, PdfBitmap> item in _buttonImages)
            {
                PdfFormField field = GetField(item.Key);
                PdfImage image = PdfImage.FromBitmap(item.Value);
                PdfReference imageReference = writer.AddObject(image.CreateStream());
                PdfArray rectangle = field.Dictionary.Get("Rect") as PdfArray;
                PdfFormXObject appearance = PdfFormXObject.FromImage(
                    rectangle, image, imageReference);
                PdfReference appearanceReference = writer.AddObject(
                    appearance.CreateStream());

                PdfDictionary appearances = new PdfDictionary();
                appearances.Set("N", appearanceReference);
                field.Dictionary.Set("AP", appearances);

                PdfDictionary iconFit = new PdfDictionary();
                iconFit.Set("SW", new PdfName("A"));
                iconFit.Set("S", new PdfName("P"));

                PdfArray alignment = new PdfArray();
                alignment.Items.Add(new PdfNumber(0.5));
                alignment.Items.Add(new PdfNumber(0.5));
                iconFit.Set("A", alignment);
                iconFit.Set("FB", new PdfBoolean(true));

                PdfDictionary appearanceCharacteristics =
                    field.Dictionary.Get("MK") as PdfDictionary ?? new PdfDictionary();
                appearanceCharacteristics.Remove("CA");
                appearanceCharacteristics.Set("I", appearanceReference);
                appearanceCharacteristics.Set("IF", iconFit);
                appearanceCharacteristics.Set("TP", new PdfNumber(1));
                field.Dictionary.Set("MK", appearanceCharacteristics);
                field.Dictionary.Set("H", new PdfName("N"));
                field.SetModified();
            }
        }

        /// <summary>
        /// Aplica al modelo PDF interno los valores de todos los campos modificados.
        /// </summary>
        /// <returns>Verdadero cuando existe al menos un campo modificado.</returns>
        private bool ApplyModifiedValues()
        {
            bool modified = false;

            foreach (PdfFormField field in _fields)
            {
                if (!field.IsModified)
                    continue;

                if (field.Reference == null)
                    throw new NotSupportedException(
                        $"El campo '{field.Name}' no está contenido en un objeto indirecto PDF.");

                if (field.IsValueModified)
                {
                    field.Dictionary.Set("V", PdfWriter.CreateUnicodeString(field.Value));
                    field.Dictionary.Remove("AP");
                }

                modified = true;
            }

            if (modified && _acroForm != null)
                _acroForm.Set("NeedAppearances", new PdfBoolean(true));

            return modified;
        }

        /// <summary>
        /// Genera el documento PDF incluyendo una revisión incremental con los valores modificados.
        /// </summary>
        /// <returns>Contenido binario del documento actualizado.</returns>
        private byte[] CreateUpdatedDocument(bool lockFields)
        {
            if (lockFields)
                LockFields();

            ApplyModifiedValues();

            PdfWriter writer = new PdfWriter(_source, _parser.Trailer, _parser.StartXref);
            ApplyButtonImages(writer);
            ApplyDocumentInformation(writer);

            foreach (PdfFormField field in _fields)
            {
                if (field.IsModified)
                    writer.SetObject(field.Reference, field.Dictionary);
            }

            if (_acroFormReference != null)
                writer.SetObject(_acroFormReference, _acroForm);

            return writer.Write();
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene los campos AcroForm encontrados en la plantilla.
        /// </summary>
        public IReadOnlyList<PdfFormField> Fields => _fields;

        #endregion

        #region Métodos Públicos Estáticos

        /// <summary>
        /// Abre una plantilla PDF desde un archivo.
        /// </summary>
        /// <param name="fileName">Ruta del archivo PDF.</param>
        /// <returns>Plantilla PDF cargada.</returns>
        /// <exception cref="ArgumentException">La ruta está vacía.</exception>
        public static PdfTemplate Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Debe indicar el archivo PDF.", nameof(fileName));

            return new PdfTemplate(File.ReadAllBytes(fileName));
        }

        /// <summary>
        /// Abre una plantilla PDF desde memoria.
        /// </summary>
        /// <param name="pdf">Contenido binario del PDF.</param>
        /// <returns>Plantilla PDF cargada.</returns>
        /// <exception cref="ArgumentNullException">El contenido es null.</exception>
        /// <exception cref="ArgumentException">El contenido está vacío.</exception>
        public static PdfTemplate Load(byte[] pdf)
        {
            if (pdf == null)
                throw new ArgumentNullException(nameof(pdf));

            if (pdf.Length == 0)
                throw new ArgumentException("El PDF está vacío.", nameof(pdf));

            return new PdfTemplate((byte[])pdf.Clone());
        }

        /// <summary>
        /// Abre una plantilla PDF desde un stream.
        /// </summary>
        /// <param name="stream">Stream que contiene el documento PDF.</param>
        /// <returns>Plantilla PDF cargada.</returns>
        /// <exception cref="ArgumentNullException">El stream es null.</exception>
        public static PdfTemplate Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (MemoryStream memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return Load(memory.ToArray());
            }
        }

        /// <summary>
        /// Abre la plantilla PDF por defecto incluida como recurso embebido.
        /// </summary>
        /// <returns>Plantilla PDF por defecto.</returns>
        /// <exception cref="InvalidOperationException">
        /// No se encuentra la plantilla PDF embebida.
        /// </exception>
        public static PdfTemplate Load()
        {
            const string resourceName = "DeCA.Resources.DeCA_DEFAULT.pdf";

            Assembly assembly = typeof(PdfTemplate).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(
                        $"No se encuentra el recurso embebido '{resourceName}'.");
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);

                    return new PdfTemplate(memoryStream.ToArray());
                }
            }
        }

        /// <summary>
        /// Guarda el documento PDF como una plantilla en el sitema de archivos.
        /// </summary>
        /// <param name="ownerPartyID"> Identificador del interlocutor que representa a la empresa
        /// propietaria del documento en el sistema de origen.</param>
        /// <param name="pdf"> Datos binarios del pdf a almacenar como plantilla.</param>
        /// <param name="name"> Nombre para la plantilla pdf.</param>
        public static void Save(string ownerPartyID, byte[] pdf, string name) 
        {

            if (string.IsNullOrWhiteSpace(ownerPartyID))
                Utils.Throw("Debe indicar el identificador del interlocutor propietario.", new ArgumentException("Debe indicar el identificador del interlocutor propietario.", nameof(ownerPartyID)));

            if (pdf == null || pdf.Length == 0)
                Utils.Throw("El contenido del PDF está vacío.", new ArgumentException("El contenido del PDF está vacío.", nameof(pdf)));

            string templatesDirectory = Path.Combine(Settings.Current.PdfTemplatePath, ownerPartyID);
            Directory.CreateDirectory(templatesDirectory);
            string templateFilePath = Path.Combine(templatesDirectory, $"{name}.pdf");

            File.WriteAllBytes(templateFilePath, pdf);

        }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Busca un campo por su nombre completo.
        /// </summary>
        /// <param name="name">Nombre completo del campo.</param>
        /// <returns>Campo encontrado o null cuando no existe.</returns>
        public PdfFormField GetField(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return null;

                _fieldsByName.TryGetValue(name, out PdfFormField field);
                return field;
            }

            /// <summary>
            /// Establece en memoria el valor textual de un campo AcroForm.
            /// El valor se incorporará al documento al invocar Save o GetBytes.
            /// </summary>
            /// <param name="fieldName">Nombre completo del campo.</param>
            /// <param name="value">Nuevo valor del campo.</param>
            /// <exception cref="ArgumentException">El nombre está vacío.</exception>
            /// <exception cref="KeyNotFoundException">El campo no existe.</exception>
            /// <exception cref="InvalidOperationException">
            /// El campo es de solo lectura o no admite valores textuales.
            /// </exception>
            public void SetValue(string fieldName, string value)
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                    throw new ArgumentException("Debe indicar el nombre del campo.", nameof(fieldName));

                PdfFormField field = GetField(fieldName);
                if (field == null)
                    throw new KeyNotFoundException(
                        $"No se encontró el campo AcroForm '{fieldName}'.");

                ValidateTextValue(field);
                field.SetValue(value);
            }

            /// <summary>
            /// Establece en memoria el valor lógico de una casilla de verificación AcroForm.
            /// El valor se incorporará al documento al invocar Save o GetBytes.
            /// </summary>
            /// <param name="fieldName">Nombre completo del campo.</param>
            /// <param name="value">Verdadero para marcar la casilla; falso para desmarcarla.</param>
            /// <exception cref="ArgumentException">El nombre está vacío.</exception>
            /// <exception cref="KeyNotFoundException">El campo no existe.</exception>
            /// <exception cref="InvalidOperationException">
            /// El campo es de solo lectura o no es una casilla de verificación.
            /// </exception>
            public void SetValue(string fieldName, bool value)
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                    throw new ArgumentException("Debe indicar el nombre del campo.", nameof(fieldName));

                PdfFormField field = GetField(fieldName);
                if (field == null)
                    throw new KeyNotFoundException(
                        $"No se encontró el campo AcroForm '{fieldName}'.");

                ValidateBooleanValue(field);

                string state = value ? GetCheckBoxOnState(field) : "Off";
                PdfName pdfState = new PdfName(state);

                field.Dictionary.Set("V", pdfState);
                field.Dictionary.Set("AS", pdfState);
                field.SetModified();
            }

            /// <summary>
            /// Establece la imagen BMP que se mostrará como apariencia normal de un campo botón.
            /// </summary>
            /// <param name="fieldName">Nombre completo del campo de formulario.</param>
            /// <param name="bitmap">Contenido binario de una imagen BMP BI_RGB de 24 o 32 bits.</param>
            /// <exception cref="ArgumentException">El nombre está vacío, el BMP no es válido o el campo no es un botón.</exception>
            /// <exception cref="ArgumentNullException">La imagen es nula.</exception>
            /// <exception cref="KeyNotFoundException">No existe el campo indicado.</exception>
            public void SetButtonImage(string fieldName, byte[] bitmap)
            {
                if (string.IsNullOrWhiteSpace(fieldName))
                    throw new ArgumentException("Debe indicar el nombre del campo.", nameof(fieldName));
                if (bitmap == null)
                    throw new ArgumentNullException(nameof(bitmap));

                PdfFormField field = GetField(fieldName);
                if (field == null)
                    throw new KeyNotFoundException(
                        $"No existe ningún campo PDF con el nombre '{fieldName}'.");
                if (field.FieldType != PdfFormFieldType.PushButton)
                    throw new ArgumentException(
                        $"El campo '{fieldName}' no es un botón de tipo PushButton.", nameof(fieldName));

                _buttonImages[fieldName] = PdfBitmap.Load((byte[])bitmap.Clone());
            }

            /// <summary>
            /// Establece la imagen BMP que se mostrará como apariencia normal de un campo botón.
            /// </summary>
            /// <param name="fieldName">Nombre completo del campo de formulario.</param>
            /// <param name="bitmap">Stream que contiene una imagen BMP BI_RGB de 24 o 32 bits.</param>
            /// <exception cref="ArgumentNullException">El stream es nulo.</exception>
            public void SetButtonImage(string fieldName, Stream bitmap)
            {
                if (bitmap == null)
                    throw new ArgumentNullException(nameof(bitmap));

                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.CopyTo(memory);
                    SetButtonImage(fieldName, memory.ToArray());
                }
            }

            /// <summary>
            /// Marca todos los campos AcroForm como de solo lectura.
            /// </summary>
            public void LockFields()
            {
                foreach (PdfFormField field in _fields)
                    field.SetReadOnly();
            }

            /// <summary>
            /// Guarda el documento PDF incluyendo los valores modificados.
            /// </summary>
            /// <param name="fileName">Ruta del archivo PDF de destino.</param>
            /// <param name="lockFields">Indica si los campos deben quedar bloqueados. El valor predeterminado es true.</param>
            /// <exception cref="ArgumentException">La ruta está vacía.</exception>
            public void Save(string fileName, bool lockFields = true)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("Debe indicar el archivo PDF de destino.", nameof(fileName));

                File.WriteAllBytes(fileName, CreateUpdatedDocument(lockFields));
            }

            /// <summary>
            /// Guarda el documento PDF incluyendo los valores modificados en un stream.
            /// </summary>
            /// <param name="stream">Stream de destino.</param>
            /// <param name="lockFields">Indica si los campos deben quedar bloqueados. El valor predeterminado es true.</param>
            /// <exception cref="ArgumentNullException">El stream es null.</exception>
            public void Save(Stream stream, bool lockFields = true)
            {
                if (stream == null)
                    throw new ArgumentNullException(nameof(stream));

                byte[] pdf = CreateUpdatedDocument(lockFields);
                stream.Write(pdf, 0, pdf.Length);
            }

            /// <summary>
            /// Obtiene el documento PDF incluyendo los valores modificados.
            /// </summary>
            /// <param name="lockFields">Indica si los campos deben quedar bloqueados. El valor predeterminado es true.</param>
            /// <returns>Contenido binario del documento actualizado.</returns>
            public byte[] GetBytes(bool lockFields = true) => CreateUpdatedDocument(lockFields);

            /// <summary>
            /// Devuelve una copia exacta del PDF original sin aplicar modificaciones.
            /// </summary>
            /// <returns>Copia del contenido binario original.</returns>
            public byte[] GetSourceBytes() => (byte[])_source.Clone();

            #endregion

    }

}
