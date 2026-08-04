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

using DeCA.Business.Data;
using DeCA.Common;
using DeCA.Config;
using DeCA.Net.Rest.Json.Parser;
using DeCA.Pdf;
using DeCA.Qrcode;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeCA.Business
{

    /// <summary>
    /// Representa una entrada en el sistema de un DeCA.
    /// </summary>
    public class DeCADocument
    {

        #region Variables Privadas Estáticas

        /// <summary>
        /// Objeto utilizado para sincronizar las operaciones realizadas
        /// sobre el contador.
        /// </summary>
        private static readonly object _Locker = new object();

        #endregion

        #region Variables Privadas de Instancia

        /// <summary>
        /// Longitud del fragmento DeCAID constituyen el nombre.
        /// Se completa con ceros a la izquierda.
        /// </summary>
        int _FileNameIdLength = 16;

        /// <summary>
        /// Longitud del fragmento versión que constituyen el nombre.
        /// Se completa con ceros a la izquierda.
        /// </summary>
        int _FileNameVersionLength = 4;

        /// <summary>
        /// Plantilla PDF utilizada para generar el PDF definitivo del documento DeCA.
        /// </summary>
        PdfTemplate _PdfTemplate;

        /// <summary>
        /// Conversor de instancia de Document a PDF.
        /// </summary>
        DeCAPdfConverter _DeCAPdfConverter;

        #endregion

        #region Propiedades Privadas de Instacia

        /// <summary>
        /// Instancia de datos gestionada por el documento DeCA.
        /// </summary>
        private readonly Document _Document;

        /// <summary>
        /// Contenido binario del PDF definitivo.
        /// </summary>
        private byte[] _Pdf;

        /// <summary>
        /// Representación JSON de la instancia de datos.
        /// </summary>
        private string _Json;

        /// <summary>
        /// Subdirectorio donde se almacenan los archivos PDF y JSON asociados al documento DeCA.
        /// </summary>
        private string SubDirectory => $"{_Document.OwnerPartyID}{Path.DirectorySeparatorChar}" +
            $"{_Document.IssueDateTime:yyyy}{Path.DirectorySeparatorChar}";

        #endregion

        #region Construtores de Instancia

        /// <summary>
        /// Constructor de la clase <see cref="DeCADocument"/> que recibe una instancia de <see cref="Document"/>.
        /// </summary>
        /// <param name="document">Instancia de <see cref="Document"</param>
        /// <exception cref="ArgumentNullException">Excepción con argumento nulo.</exception>
        public DeCADocument(Document document)
        {

            if (document == null)
                throw new ArgumentNullException(nameof(document));

            _Document = document;

            var erors = GetErrors();

            if (erors.Count > 0)
            {

                var errMsg = $"El documento DeCA no es válido: {string.Join("\n", erors)}";
                Utils.Throw(errMsg, new ArgumentException(errMsg));

            }

        }

        #endregion

        #region Métodos Privados de Instancia

        /// <summary>
        /// Cambia el documento DeCA ya creado en el sistema de archivos, generando el PDF definitivo y la representación JSON.
        /// </summary>
        private void Change()
        {

            if (!File.Exists(PdfFilePath))
                Utils.Throw($"No existe el documento con id '{_Document.DeCAID}'.",
                    new FileNotFoundException($"No existe el documento con id '{_Document.DeCAID}'."));

            lock (_Locker)
            {

                var filesVersions = Directory.GetFiles(Path.GetDirectoryName(JsonFilePath), $"{_Document.DeCAID}.*.json");
                _Document.Version = filesVersions.Length;

                var json = File.ReadAllText(filesVersions[0]);
                var jsonParser = new JsonParser(json);
                var result = jsonParser.GetResult<Document>();

                _Document.CreationDateTime = result.CreationDateTime;

            }            

            if (string.IsNullOrEmpty($"{_Document.DocumentNumber}".Trim()))
                _Document.DocumentNumber = _Document.DeCAID;


            Create(false);

        }

        /// <summary>
        /// Crea el documento DeCA en el sistema de archivos,
        /// generando el PDF definitivo y la representación JSON.
        /// </summary>
        private void Create(bool setId = true)
        {

            if (setId)
                SetID();

            SetUrl();
            SetTimeLife(setId ? null : _Document.CreationDateTime);
            SetPdf();
            _Json = _Document.ToJson();

            Directory.CreateDirectory(Path.GetDirectoryName(PdfFilePath));
            Directory.CreateDirectory(Path.GetDirectoryName(JsonFilePath));

            File.WriteAllBytes(PdfFilePath, _Pdf);
            File.WriteAllText(JsonFilePath, _Json);

        }

        /// <summary>
        /// Devuelve una lista con los errores de validación del documento DeCA.
        /// </summary>
        /// <returns>Lista con los errores de validación del documento DeCA.</returns>
        private List<string> GetErrors()
        {

            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty($"{_Document.OwnerPartyID}".Trim()))
                errors.Add("OwnerPartyID no puede ser nulo o vacío.");

            if (_Document.Version < 0)
                errors.Add("Version no puede ser un número negativo.");

            if (_Document.IssueDateTime == default)
                errors.Add("IssueDateTime no puede ser la fecha por defecto.");

            if (_Document.Parties == null)
                errors.Add("Parties no puede ser nulo.");

            var partyRoles = new List<string> { "CC", "TE", "EX", "DS", "OR", "DE", "OT" };
            var mandatoryPartyRoles = new List<string> { "CC", "TE", "OR", "DE" };
            var unknownPartyRoles = new List<string>();
            var partyRolesCount = new Dictionary<string, int>();

            foreach (var party in _Document.Parties)
            {

                if (partyRolesCount.ContainsKey(party.PartyRole))
                    partyRolesCount[party.PartyRole]++;
                else
                    partyRolesCount[party.PartyRole] = 1;

                if (!partyRoles.Contains(party.PartyRole))
                    unknownPartyRoles.Add(party.PartyRole);

                if (mandatoryPartyRoles.Contains(party.PartyRole))
                    mandatoryPartyRoles.Remove(party.PartyRole);

            }

            foreach (var role in new string[] { "CC", "TE", "OR", "DE" })
                if (partyRolesCount.ContainsKey(role) && partyRolesCount[role] > 1)
                    errors.Add($"El rol {role} únicamente se puede utilizar una vez el Parties y se ha econtrado {partyRolesCount[role]} veces.");

            if (mandatoryPartyRoles.Count > 0)
                errors.Add($"Faltan los siguienetes roles de party obligatorios: {string.Join(",", mandatoryPartyRoles)}.");

            if (unknownPartyRoles.Count > 0)
                errors.Add($"Encontrados los siguienetes roles de party desconocidos: {string.Join(",", unknownPartyRoles)}.");

            if (string.IsNullOrEmpty($"{_Document.GoodsDescription}".Trim()))
                errors.Add("GoodsDescription no puede ser nulo o vacío.");

            if (_Document.GrossWeight <= 0)
                errors.Add("GrossWeight debe se mayor que 0.");

            if (!string.IsNullOrEmpty($"{_Document.DownloadURL}".Trim()))
                errors.Add("El documento DeCA ya tiene asignado un DownloadURL.");

            return errors;

        }

        /// <summary>
        /// Asigna un identificador único al documento DeCA.
        /// </summary>
        private void SetID()
        {

            if (!string.IsNullOrEmpty($"{_Document.DeCAID}".Trim()))
                Utils.Throw("El documento DeCA ya tiene asignado un DeCAID.", new ArgumentException("El documento DeCA ya tiene asignado un DeCAID."));

            var counter = DeCADocumentCounter.Get(_Document.OwnerPartyID, _Document.IssueDateTime ?? default);
            _Document.DeCAID = $"{counter.Next()}".PadLeft(_FileNameIdLength, '0');

            if (string.IsNullOrEmpty($"{_Document.DocumentNumber}".Trim()))
                _Document.DocumentNumber = _Document.DeCAID;

        }

        /// <summary>
        /// Asigna la URL de descarga única al documento DeCA.
        /// </summary>
        private void SetUrl()
        {

            if (!string.IsNullOrEmpty($"{_Document.DownloadURL}".Trim()))
                Utils.Throw("El documento DeCA ya tiene asignado un DownloadURL.", new ArgumentException("El documento DeCA ya tiene asignado un DownloadURL."));

            _Document.DownloadURL = _Document.QRCodeValue = DownloadURL;
            _Document.QRCode = _DeCAPdfConverter.QrCode;

        }

        /// <summary>
        /// Asigna los valores de tiempo de vida al documento DeCA.
        /// </summary>
        private void SetTimeLife(DateTime? creation = null)
        {

            var now = DateTime.Now;

            _Document.CreationDateTime = creation??now;
            _Document.ModificationDateTime = now;
            _Document.DownloadAvailableFromDateTime = now;
            _Document.DownloadAvailableUntilDateTime = now.AddYears(1);
            _Document.Status = "DEFINITIVO";

        }

        /// <summary>
        /// Genera el PDF definitivo del documento DeCA a partir de la plantilla incluida en la librería.
        /// </summary>
        private void SetPdf()
        {

            if (_Pdf != null)
                Utils.Throw("El documento DeCA ya tiene asignado un PDF.", new ArgumentException("El documento DeCA ya tiene asignado un PDF."));

            // Generar el PDF utilizando la plantilla incluida en la librería.
            LoadPdfTemplate();
            _DeCAPdfConverter = new DeCAPdfConverter(_Document, _PdfTemplate);

            _Pdf = _DeCAPdfConverter.GetPdf();
            _Document.FileHash = BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(_Pdf)).Replace("-", "");
            _Document.FileSize = _Pdf.Length;
            _Document.FileName = $"{FileName}.pdf";

        }

        /// <summary>
        /// Carga la plantilla PDF utilizada para generar el PDF definitivo del documento DeCA.
        /// </summary>
        /// <returns> Plantilla PDF utilizada para generar el PDF definitivo del documento DeCA.</returns>
        private PdfTemplate LoadPdfTemplate() 
        {

            // Generar el PDF utilizando la plantilla incluida en la librería.
            PdfTemplate pdfTemplate = SourcePdfTemplate == null ? null : new PdfTemplate(SourcePdfTemplate);

            if (pdfTemplate == null && SourcePdfFileName != null)
            {

                string templatesDirectory = Path.Combine(Settings.Current.PdfTemplatePath, _Document.OwnerPartyID);
                string templateFilePath = Path.Combine(templatesDirectory, $"{SourcePdfFileName}.pdf");

                if (!File.Exists(templateFilePath))
                    Utils.Throw($"No se ha encontrado la plantilla PDF '{SourcePdfFileName}' para el propietario '{_Document.OwnerPartyID}'.",
                        new FileNotFoundException($"No se ha encontrado la plantilla PDF '{SourcePdfFileName}' para el propietario '{_Document.OwnerPartyID}'."));

                pdfTemplate = new PdfTemplate(File.ReadAllBytes(SourcePdfFileName));

            }

            _PdfTemplate = pdfTemplate;

            return pdfTemplate;


        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// URL HTTPS única que permite descargar directamente el PDF.
        /// </summary>
        public string DownloadURL => $"{Settings.Current.DECaSettings.DECaEndPointPrefix}?dc={_Document.DeCAID}";

        /// <summary>
        /// Nombre sin estexsión para los archivos PDF y JSON asociados al documento DeCA.
        /// </summary>
        public string FileName => $"{_Document.DeCAID}".PadLeft(_FileNameIdLength, '0') + "." +
            $"{_Document.Version}".PadLeft(_FileNameVersionLength, '0');

        /// <summary>
        /// Ruta completa del archivo PDF definitivo en el sistema de archivos.
        /// </summary>
        public string PdfFilePath => $"{Settings.Current.PdfPath}{SubDirectory}{FileName}.pdf";

        /// <summary>
        /// Ruta completa del archivo JSON definitivo en el sistema de archivos.
        /// </summary>
        public string JsonFilePath => $"{Settings.Current.JsonPath}{SubDirectory}{FileName}.json";

        /// <summary>
        /// Contenido incorporado al código QR.
        /// Normalmente coincidirá con la URL de descarga.
        /// </summary>
        public string QRCodeValue => DownloadURL;

        /// <summary>
        /// Datos binarios de la plantilla PDF utilizada para generar el PDF definitivo del documento DeCA.
        /// </summary>
        public byte[] SourcePdfTemplate { get;set; }

        /// <summary>
        /// Nombre archivo de la plantilla PDF utilizada para generar el PDF definitivo del documento DeCA.
        /// </summary>
        public string SourcePdfFileName { get; set; }

        #endregion

        #region Métodos Públicos Estáticos

        /// <summary>
        /// Carga un documento DeCA desde el sistema de archivos a partir de su identificador único.
        /// </summary>
        /// <param name="ownerPartyID"> Identificador del interlocutor que representa a la empresa
        /// propietaria del documento en el sistema de origen.</param>
        /// <param name="issueYear">Año de emisión.</param>
        /// <param name="decaID">Identificador único del documento electrónico de control.</param>
        /// <returns>Última versión del documento.</returns>
        public static Document LoadDocument(string ownerPartyID, string issueYear, string decaID)
        {

            string subDirectory = Path.Combine(ownerPartyID, issueYear.ToString());
            string jsonDirectory = Path.Combine(Settings.Current.JsonPath, subDirectory);
            string searchPattern = $"{decaID}.*.json";

            string[] files = Directory.GetFiles(jsonDirectory, searchPattern, SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
                Utils.Throw($"No se ha encontrado ningún documento DeCA con id '{decaID}' para el interlocutor '{ownerPartyID}' y año de emisión '{issueYear}'.",
                    new FileNotFoundException($"No se ha encontrado ningún documento DeCA con id '{decaID}' para el interlocutor '{ownerPartyID}' y año de emisión '{issueYear}'."));

            Array.Sort(files);

            var currentVersionFile = files[files.Length - 1];

            var json = File.ReadAllText(currentVersionFile);
            var jsonParser = new JsonParser(json);

            return jsonParser.GetResult<Document>();

        }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Guarda el documento DeCA en el sistema de archivos, generando el PDF definitivo y la representación JSON.
        /// </summary>
        public void Save()
        {

            if (string.IsNullOrEmpty($"{_Document.DeCAID}".Trim()))
                Create();
            else
                Change();

        }

        /// <summary>
        /// Devuelve una cadena que representa el documento DeCA.
        /// </summary>
        /// <returns> Cadena que representa el documento DeCA.</returns>
        public override string ToString()
        {

            return $"{_Document.OwnerPartyID}, {_Document.IssueDateTime:yyyy-MM-dd}, {_Document.DeCAID}";

        }

        #endregion

    }

}