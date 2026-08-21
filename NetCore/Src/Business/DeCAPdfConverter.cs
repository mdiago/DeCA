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
    serving DECa services on the fly in a web application,
    shipping DECa with a closed source product.

    For more information, please contact Irene Solutions SL. at this
    address: info@irenesolutions.com
 */

using DeCA.Common;
using DeCA.Pdf;
using DeCA.Business.Data;
using System;
using System.Collections.Generic;

namespace DeCA.Business
{

    /// <summary>
    /// Conversor de instancia de Document a PDF.
    /// </summary>
    public class DeCAPdfConverter
    {

        #region Variables Privadas Estáticas

        /// <summary>
        /// Formatos de los tipos de datos para el PDF.
        /// </summary>
        private static readonly Dictionary<Type, string> _TypeFormats = new Dictionary<Type, string>()
        {
            { typeof(DateTime),     "yyyy-MM-dd HH:mm:ss" },
            { typeof(DateTime?),    "yyyy-MM-dd HH:mm:ss" },
            { typeof(decimal),      "#,##0.00" },
            { typeof(decimal?),     "#,##0.00" },
            { typeof(int),          "#,##0" },
            { typeof(int?),         "#,##0" }
        };

        #endregion

        #region Variables Privadas de Instancia

        /// <summary>
        /// Documento a convertir a PDF.
        /// </summary>
        private readonly Document _Document;

        /// <summary>
        /// Plantilla PDF.
        /// </summary>
        private readonly PdfTemplate _PdfTemplate;

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="document">Documento a convertir a PDF.</param>
        /// <param name="pdfTemplate">Plantilla PDF.</param>
        public DeCAPdfConverter(Document document, PdfTemplate pdfTemplate = null)
        {

            if (document == null)
                throw new ArgumentNullException(nameof(document));

            _Document = document;

            if (pdfTemplate == null)
                _PdfTemplate = PdfTemplate.Load();
            else
                _PdfTemplate = pdfTemplate;

        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Datos binarios del bitmap del código QR del documento.
        /// </summary>
        public byte[] QrCode { get; private set; }

        /// <summary>
        /// Datos binarios del PDF del documento.
        /// </summary>
        public byte[] Pdf { get; private set; }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Obtiene el pdf del documento.
        /// </summary>
        /// <param name="propsFormats">Diccionario de formatos personalizados para propiedades.</param>
        /// <returns>PDF del documento DeCA.</returns>
        public byte[] GetPdf(Dictionary<string, string> propsFormats = null)
        {

            foreach (var p in _Document.GetType().GetProperties())
            {
                var value = p.GetValue(_Document);
                var name = p.Name;
                var parties = value as IList<Party>;

                try
                {


                    if (parties == null)
                    {

                        string pdfValue = $"{value}";

                        if (propsFormats != null && propsFormats.ContainsKey(name))
                        {

                            var format = propsFormats[name];
                            pdfValue = string.Format("{0:" + format + "}", value);

                        }
                        else if (_TypeFormats.ContainsKey(p.PropertyType))
                        {

                            var format = _TypeFormats[p.PropertyType];
                            pdfValue = string.Format("{0:" + format + "}", value);

                        }

                        if (p.PropertyType == typeof(bool))
                            _PdfTemplate.SetValue(name, (bool)value);
                        else
                            _PdfTemplate.SetValue(name, pdfValue);

                    }
                    else
                    {

                        foreach (var party in parties)
                        {
                            foreach (var pp in party.GetType().GetProperties())
                            {
                                var pvalue = pp.GetValue(party);
                                var pname = pp.Name;

                                try
                                {
                                    _PdfTemplate.SetValue($"{party.PartyRole}.{pname}", $"{pvalue}");
                                }
                                catch (KeyNotFoundException nkEx)
                                {
                                    Utils.Logger.Log($"Error en TestPdfFromResourcesQr: {nkEx.Message}");
                                }
                            }
                        }

                    }

                }
                catch (KeyNotFoundException nkEx)
                {

                    Utils.Logger.Log($"Error al asignar un campo PDF: {nkEx.Message}");

                }
                catch (Exception ex)
                {

                    throw new Exception($"Error al generar el PDF del documento DeCA: {ex.Message}", ex);

                }

            }

            // Inserto Qr
            QrCode = QrCodeGenerator.GetQr(_Document.QRCodeValue);

            _PdfTemplate.SetButtonImage(
                "QRCodeImage",
                QrCode);


            Pdf = _PdfTemplate.GetBytes();

            return Pdf;

        }

        #endregion

    }

}