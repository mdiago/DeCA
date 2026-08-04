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
using DeCA.Pdf.Images;
using System;
using System.Globalization;
using System.Text;

namespace DeCA.Pdf.Appearance
{

    /// <summary>
    /// Representa un Form XObject que dibuja una imagen dentro de un rectángulo PDF.
    /// </summary>
    internal sealed class PdfFormXObject
    {

        #region Variables Privadas de Instancia

        /// <summary>
        /// Anchura del formulario en puntos PDF.
        /// </summary>
        private readonly double _width;

        /// <summary>
        /// Altura del formulario en puntos PDF.
        /// </summary>
        private readonly double _height;

        /// <summary>
        /// Imagen que se dibujará dentro del formulario.
        /// </summary>
        private readonly PdfImage _image;

        /// <summary>
        /// Referencia indirecta al XObject de imagen.
        /// </summary>
        private readonly PdfReference _imageReference;

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa un Form XObject para dibujar una imagen.
        /// </summary>
        /// <param name="width">Anchura del formulario en puntos PDF.</param>
        /// <param name="height">Altura del formulario en puntos PDF.</param>
        /// <param name="image">Imagen que se dibujará.</param>
        /// <param name="imageReference">Referencia indirecta al XObject de imagen.</param>
        private PdfFormXObject(double width, double height, PdfImage image,
            PdfReference imageReference)
        {
            _width = width;
            _height = height;
            _image = image ?? throw new ArgumentNullException(nameof(image));
            _imageReference = imageReference ??
                throw new ArgumentNullException(nameof(imageReference));
        }

        #endregion

        #region Métodos Privados Estáticos

        /// <summary>
        /// Añade un número al array PDF indicado.
        /// </summary>
        /// <param name="array">Array PDF que recibirá el número.</param>
        /// <param name="value">Valor numérico.</param>
        private static void AddNumber(PdfArray array, double value)
        {
            array.Items.Add(new PdfNumber(value));
        }

        /// <summary>
        /// Representa un número usando cultura invariante.
        /// </summary>
        /// <param name="value">Número que se desea representar.</param>
        /// <returns>Representación textual del número.</returns>
        private static string FormatNumber(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Obtiene una dimensión del rectángulo PDF indicado.
        /// </summary>
        /// <param name="rectangle">Rectángulo PDF.</param>
        /// <param name="firstIndex">Índice de la coordenada inicial.</param>
        /// <param name="secondIndex">Índice de la coordenada final.</param>
        /// <returns>Dimensión positiva calculada.</returns>
        private static double GetDimension(PdfArray rectangle, int firstIndex, int secondIndex)
        {
            if (rectangle == null || rectangle.Items.Count < 4 ||
                !(rectangle.Items[firstIndex] is PdfNumber first) ||
                !(rectangle.Items[secondIndex] is PdfNumber second))
                throw new InvalidOperationException(
                    "El campo de botón no contiene un rectángulo /Rect válido.");

            double result = Math.Abs(second.Value - first.Value);
            if (result <= 0)
                throw new InvalidOperationException(
                    "El campo de botón tiene un tamaño no válido.");

            return result;
        }

        #endregion

        #region Métodos Públicos Estáticos

        /// <summary>
        /// Crea un Form XObject que ajusta proporcionalmente una imagen al rectángulo indicado.
        /// </summary>
        /// <param name="rectangle">Rectángulo PDF donde se mostrará la imagen.</param>
        /// <param name="image">Imagen que se desea dibujar.</param>
        /// <param name="imageReference">Referencia indirecta al XObject de imagen.</param>
        /// <returns>Formulario preparado para crear la apariencia del botón.</returns>
        internal static PdfFormXObject FromImage(PdfArray rectangle, PdfImage image,
            PdfReference imageReference)
        {
            double width = GetDimension(rectangle, 0, 2);
            double height = GetDimension(rectangle, 1, 3);
            return new PdfFormXObject(width, height, image, imageReference);
        }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Crea el stream PDF que dibuja la imagen dentro del formulario.
        /// </summary>
        /// <returns>Stream PDF del Form XObject.</returns>
        internal PdfStream CreateStream()
        {
            double scale = Math.Min(_width / _image.Width, _height / _image.Height);
            double imageWidth = _image.Width * scale;
            double imageHeight = _image.Height * scale;
            double horizontalPosition = (_width - imageWidth) / 2;
            double verticalPosition = (_height - imageHeight) / 2;

            PdfArray boundingBox = new PdfArray();
            AddNumber(boundingBox, 0);
            AddNumber(boundingBox, 0);
            AddNumber(boundingBox, _width);
            AddNumber(boundingBox, _height);

            PdfDictionary xObjects = new PdfDictionary();
            xObjects.Set("QrImage", _imageReference);

            PdfDictionary resources = new PdfDictionary();
            resources.Set("XObject", xObjects);

            PdfDictionary dictionary = new PdfDictionary();
            dictionary.Set("Type", new PdfName("XObject"));
            dictionary.Set("Subtype", new PdfName("Form"));
            dictionary.Set("FormType", new PdfNumber(1));
            dictionary.Set("BBox", boundingBox);
            dictionary.Set("Resources", resources);

            string commands = "q\n" +
                FormatNumber(imageWidth) + " 0 0 " + FormatNumber(imageHeight) + " " +
                FormatNumber(horizontalPosition) + " " + FormatNumber(verticalPosition) + " cm\n" +
                "/QrImage Do\nQ";

            return new PdfStream(dictionary, Encoding.ASCII.GetBytes(commands));
        }

        #endregion

    }

}
