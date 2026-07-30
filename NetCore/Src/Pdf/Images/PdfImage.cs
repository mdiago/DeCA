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

using DeCA.Pdf.Core;

namespace DeCA.Pdf.Images
{

    /// <summary>
    /// Representa una imagen preparada para escribirse como un XObject de imagen PDF.
    /// </summary>
    internal sealed class PdfImage
    {

        #region Variables Privadas de Instancia

        /// <summary>
        /// Imagen bitmap de origen convertida a píxeles RGB.
        /// </summary>
        private readonly PdfBitmap _bitmap;

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa una imagen PDF a partir de una imagen bitmap preparada.
        /// </summary>
        /// <param name="bitmap">Imagen bitmap convertida a RGB.</param>
        private PdfImage(PdfBitmap bitmap)
        {
            _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene la anchura de la imagen en píxeles.
        /// </summary>
        internal int Width => _bitmap.Width;

        /// <summary>
        /// Obtiene la altura de la imagen en píxeles.
        /// </summary>
        internal int Height => _bitmap.Height;

        #endregion

        #region Métodos Públicos Estáticos

        /// <summary>
        /// Crea una imagen PDF a partir de una imagen bitmap preparada.
        /// </summary>
        /// <param name="bitmap">Imagen bitmap convertida a RGB.</param>
        /// <returns>Imagen preparada para su escritura en el documento PDF.</returns>
        /// <exception cref="ArgumentNullException">La imagen es nula.</exception>
        internal static PdfImage FromBitmap(PdfBitmap bitmap)
        {
            return new PdfImage(bitmap);
        }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Crea el stream que representa el XObject de imagen PDF.
        /// </summary>
        /// <returns>Stream PDF con los píxeles RGB de la imagen.</returns>
        internal PdfStream CreateStream()
        {
            PdfDictionary dictionary = new PdfDictionary();
            dictionary.Set("Type", new PdfName("XObject"));
            dictionary.Set("Subtype", new PdfName("Image"));
            dictionary.Set("Width", new PdfNumber(Width));
            dictionary.Set("Height", new PdfNumber(Height));
            dictionary.Set("ColorSpace", new PdfName("DeviceRGB"));
            dictionary.Set("BitsPerComponent", new PdfNumber(8));

            return new PdfStream(dictionary, _bitmap.RgbData);
        }

        #endregion

    }

}
