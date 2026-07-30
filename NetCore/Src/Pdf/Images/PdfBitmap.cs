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

namespace DeCA.Pdf.Images
{

    /// <summary>
    /// Representa una imagen BMP convertida al formato RGB utilizado por PDF.
    /// </summary>
    internal sealed class PdfBitmap
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa una imagen bitmap preparada para su inclusión en un PDF.
        /// </summary>
        /// <param name="width">Anchura de la imagen en píxeles.</param>
        /// <param name="height">Altura de la imagen en píxeles.</param>
        /// <param name="rgbData">Píxeles RGB ordenados desde la fila superior.</param>
        private PdfBitmap(int width, int height, byte[] rgbData)
        {
            Width = width;
            Height = height;
            RgbData = rgbData;
        }

        #endregion

        #region Métodos Privados Estáticos

        /// <summary>
        /// Lee un entero de 16 bits sin signo en orden little-endian.
        /// </summary>
        /// <param name="data">Contenido binario del BMP.</param>
        /// <param name="offset">Posición inicial del entero.</param>
        /// <returns>Valor leído.</returns>
        private static ushort ReadUInt16(byte[] data, int offset)
        {
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }

        /// <summary>
        /// Lee un entero de 32 bits con signo en orden little-endian.
        /// </summary>
        /// <param name="data">Contenido binario del BMP.</param>
        /// <param name="offset">Posición inicial del entero.</param>
        /// <returns>Valor leído.</returns>
        private static int ReadInt32(byte[] data, int offset)
        {
            return data[offset] |
                (data[offset + 1] << 8) |
                (data[offset + 2] << 16) |
                (data[offset + 3] << 24);
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene la anchura de la imagen en píxeles.
        /// </summary>
        internal int Width { get; }

        /// <summary>
        /// Obtiene la altura de la imagen en píxeles.
        /// </summary>
        internal int Height { get; }

        /// <summary>
        /// Obtiene los píxeles RGB ordenados desde la fila superior.
        /// </summary>
        internal byte[] RgbData { get; }

        #endregion

        #region Métodos Públicos Estáticos

        /// <summary>
        /// Interpreta una imagen BMP sin compresión de 24 o 32 bits.
        /// </summary>
        /// <param name="bitmap">Contenido binario completo de la imagen BMP.</param>
        /// <returns>Imagen convertida al formato RGB utilizado por PDF.</returns>
        /// <exception cref="ArgumentNullException">La imagen es nula.</exception>
        /// <exception cref="ArgumentException">La imagen no es un BMP compatible.</exception>
        internal static PdfBitmap Load(byte[] bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            if (bitmap.Length < 54 || bitmap[0] != (byte)'B' || bitmap[1] != (byte)'M')
                throw new ArgumentException("El contenido no representa una imagen BMP válida.", nameof(bitmap));

            int pixelOffset = ReadInt32(bitmap, 10);
            int dibHeaderSize = ReadInt32(bitmap, 14);
            int width = ReadInt32(bitmap, 18);
            int signedHeight = ReadInt32(bitmap, 22);
            ushort planes = ReadUInt16(bitmap, 26);
            ushort bitsPerPixel = ReadUInt16(bitmap, 28);
            int compression = ReadInt32(bitmap, 30);

            if (dibHeaderSize < 40 || width <= 0 || signedHeight == 0 || planes != 1 ||
                (bitsPerPixel != 24 && bitsPerPixel != 32) || compression != 0)
                throw new ArgumentException(
                    "Sólo se admiten imágenes BMP BI_RGB de 24 o 32 bits.", nameof(bitmap));

            int height = Math.Abs(signedHeight);
            bool topDown = signedHeight < 0;
            int bytesPerPixel = bitsPerPixel / 8;
            int sourceStride = checked(((width * bytesPerPixel + 3) / 4) * 4);
            long requiredLength = (long)pixelOffset + ((long)sourceStride * height);

            if (pixelOffset < 0 || requiredLength > bitmap.Length)
                throw new ArgumentException("La información de píxeles del BMP está incompleta.", nameof(bitmap));

            byte[] rgbData = new byte[checked(width * height * 3)];

            for (int targetRow = 0; targetRow < height; targetRow++)
            {
                int sourceRow = topDown ? targetRow : height - targetRow - 1;
                int sourcePosition = pixelOffset + sourceRow * sourceStride;
                int targetPosition = targetRow * width * 3;

                for (int column = 0; column < width; column++)
                {
                    int sourcePixel = sourcePosition + column * bytesPerPixel;
                    int targetPixel = targetPosition + column * 3;
                    rgbData[targetPixel] = bitmap[sourcePixel + 2];
                    rgbData[targetPixel + 1] = bitmap[sourcePixel + 1];
                    rgbData[targetPixel + 2] = bitmap[sourcePixel];
                }
            }

            return new PdfBitmap(width, height, rgbData);
        }

        #endregion

    }

}
