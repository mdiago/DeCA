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
    Boston, MA 02110-1301 USA, or download the license from the following URL:
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

using DECa.Pdf.Core;
using System;

namespace DECa.Pdf.Forms
{

    /// <summary>
    /// Tipos de campo AcroForm soportados por la librería.
    /// </summary>
    public enum PdfFormFieldType
    {
        Unknown,
        Text,
        Button,
        CheckBox,
        RadioButton,
        PushButton,
        Choice,
        Signature
    }

    /// <summary>
    /// Representa el estado de un campo AcroForm de una plantilla PDF.
    /// </summary>
    public sealed class PdfFormField
    {

        #region Variables Privadas de Instancia

        /// <summary>
        /// Valor actual del campo en memoria.
        /// </summary>
        private string _value;

        /// <summary>
        /// Indica si el valor del campo ha sido modificado desde la carga del documento.
        /// </summary>
        private bool _isValueModified;

        /// <summary>
        /// Indica si el diccionario PDF del campo ha sido modificado.
        /// </summary>
        private bool _isModified;

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa la información y el estado de un campo AcroForm.
        /// </summary>
        /// <param name="name">Nombre completo del campo.</param>
        /// <param name="partialName">Nombre parcial definido en el elemento actual.</param>
        /// <param name="fieldType">Tipo del campo.</param>
        /// <param name="flags">Indicadores AcroForm del campo.</param>
        /// <param name="value">Valor inicial del campo.</param>
        /// <param name="dictionary">Diccionario PDF asociado al campo.</param>
        /// <param name="reference">Referencia indirecta al objeto PDF del campo, si existe.</param>
        internal PdfFormField(string name, string partialName, PdfFormFieldType fieldType,
            int flags, string value, PdfDictionary dictionary, PdfReference reference)
        {
            Name = name;
            PartialName = partialName;
            FieldType = fieldType;
            Flags = flags;
            _value = value;
            _isValueModified = false;
            _isModified = false;
            Dictionary = dictionary;
            Reference = reference;
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el nombre completo del campo, incluyendo sus campos padre.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Obtiene el nombre parcial definido mediante la entrada /T.
        /// </summary>
        public string PartialName { get; }

        /// <summary>
        /// Obtiene el tipo del campo.
        /// </summary>
        public PdfFormFieldType FieldType { get; }

        /// <summary>
        /// Obtiene los indicadores AcroForm definidos mediante la entrada /Ff.
        /// </summary>
        public int Flags { get; private set; }

        /// <summary>
        /// Obtiene el valor actual del campo en memoria.
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// Indica si el campo está marcado como de solo lectura.
        /// </summary>
        public bool IsReadOnly => (Flags & 1) != 0;

        /// <summary>
        /// Indica si el campo de texto admite varias líneas.
        /// </summary>
        public bool IsMultiline => FieldType == PdfFormFieldType.Text && (Flags & 4096) != 0;

        /// <summary>
        /// Obtiene el diccionario PDF asociado al campo.
        /// </summary>
        internal PdfDictionary Dictionary { get; }

        /// <summary>
        /// Obtiene la referencia indirecta al objeto PDF asociado al campo, si existe.
        /// </summary>
        internal PdfReference Reference { get; }

        /// <summary>
        /// Indica si el valor del campo ha sido modificado desde la carga del documento.
        /// </summary>
        internal bool IsValueModified => _isValueModified;

        /// <summary>
        /// Indica si el diccionario PDF del campo ha sido modificado.
        /// </summary>
        internal bool IsModified => _isModified;

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Establece el valor actual del campo en memoria.
        /// </summary>
        /// <param name="value">Nuevo valor del campo.</param>
        internal void SetValue(string value)
        {
            string normalizedValue = value ?? string.Empty;
            if (string.Equals(_value ?? string.Empty, normalizedValue, StringComparison.Ordinal))
                return;

            _value = normalizedValue;
            _isValueModified = true;
            _isModified = true;
        }

        /// <summary>
        /// Marca el campo como de solo lectura en el diccionario AcroForm.
        /// </summary>
        internal void SetReadOnly()
        {
            if (IsReadOnly)
                return;

            Flags |= 1;
            Dictionary.Set("Ff", new PdfNumber(Flags));
            _isModified = true;
        }

        /// <summary>
        /// Devuelve una representación textual del campo.
        /// </summary>
        /// <returns>Nombre y tipo del campo.</returns>
        public override string ToString() => $"{Name} ({FieldType})";

        #endregion

    }

}
