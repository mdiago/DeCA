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

        /// <summary>
        /// Documento a convertir a PDF.
        /// </summary>
        Document _Document;

        PdfTemplate _PdfTemplate;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="document">Documento a convertir a PDF.</param>
        /// <param name="pdfTemplate">Plantilla PDF.</param>
        public DeCAPdfConverter(Document document, PdfTemplate pdfTemplate=null)
        {

            if(document == null)
                throw new ArgumentNullException(nameof(document));

            _Document = document;

            if (pdfTemplate == null)
                _PdfTemplate = PdfTemplate.Load();
            else
                _PdfTemplate = pdfTemplate;

        }

        /// <summary>
        /// Obtiene el pdf del documento.
        /// </summary>
        /// <returns>PDF del documento DeCA.</returns>
        public byte[] GetPdf() 
        { 
        
            foreach(var p in _Document.GetType().GetProperties())
            {
                var value = p.GetValue(_Document);
                var name = p.Name;
                var parties = value as IList<Party>;

                try
                {


                    if (parties == null) 
                    {

                        if(p.PropertyType == typeof(string))
                            _PdfTemplate.SetValue(name, $"{value}");
                        else if (p.PropertyType == typeof(bool))
                            _PdfTemplate.SetValue(name, (bool)value);
                        else if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                            _PdfTemplate.SetValue(name, $"{value:yyyy-MM-dd HH:mm:ss}");
                        else if (p.PropertyType == typeof(decimal)|| p.PropertyType == typeof(decimal?))
                            _PdfTemplate.SetValue(name, $"{value:#,##0.00}");
                        else if (p.PropertyType == typeof(int) || p.PropertyType == typeof(int?))
                            _PdfTemplate.SetValue(name, $"{value:#,##0}");

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

                    // Inserto Qr
                    QrCode = QrCodeGenerator.GetQr(_Document.QRCodeValue);

                    _PdfTemplate.SetButtonImage(
                        "QRCodeImage",
                        QrCode);

                }
                catch (KeyNotFoundException nkEx)
                {

                    Utils.Logger.Log($"Error en TestPdfFromResourcesQr: {nkEx.Message}");

                }
                catch (Exception ex)
                {

                    throw new Exception($"Error en TestPdfFromResourcesQr: {ex.Message}", ex);

                }

            }

            Pdf = _PdfTemplate.GetBytes();

            return Pdf;

        }

        /// <summary>
        /// Datos binarios del bitmap del código QR del documento.
        /// </summary>
        public byte[] QrCode { get; private set; }

        /// <summary>
        /// Datos binarios del PDF del documento.
        /// </summary>
        public byte[] Pdf { get; private set; }

    }

}