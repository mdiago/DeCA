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

using System.Globalization;
using System.Text;

namespace DECa.Pdf.Core
{

    /// <summary>
    /// Escribe una nueva revisión incremental sobre un documento PDF existente.
    /// </summary>
    internal sealed class PdfWriter
    {

        #region Variables Privadas Estáticas

        /// <summary>
        /// Codificación de un solo byte utilizada por la sintaxis estructural PDF.
        /// </summary>
        private static readonly Encoding PdfEncoding = Encoding.GetEncoding("ISO-8859-1");

        #endregion

        #region Variables Privadas de Instancia

        /// <summary>
        /// Contenido binario de la revisión original del documento.
        /// </summary>
        private readonly byte[] _source;

        /// <summary>
        /// Trailer correspondiente a la revisión original.
        /// </summary>
        private readonly PdfDictionary _trailer;

        /// <summary>
        /// Posición de la tabla xref de la revisión original.
        /// </summary>
        private readonly long _previousXref;

        /// <summary>
        /// Objetos indirectos que se escribirán en la nueva revisión.
        /// </summary>
        private readonly SortedDictionary<int, PdfIndirectObject> _objects =
            new SortedDictionary<int, PdfIndirectObject>();

        /// <summary>
        /// Valores del trailer que deben sustituirse en la nueva revisión.
        /// </summary>
        private readonly Dictionary<string, PdfObject> _trailerValues =
            new Dictionary<string, PdfObject>(StringComparer.Ordinal);

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa el escritor incremental.
        /// </summary>
        /// <param name="source">Contenido binario de la revisión original.</param>
        /// <param name="trailer">Trailer de la revisión original.</param>
        /// <param name="previousXref">Posición de la tabla xref anterior.</param>
        internal PdfWriter(byte[] source, PdfDictionary trailer, long previousXref)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _trailer = trailer ?? throw new ArgumentNullException(nameof(trailer));
            _previousXref = previousXref;
        }

        #endregion

        #region Métodos Privados Estáticos

        /// <summary>
        /// Convierte una cadena .NET en una cadena hexadecimal PDF Unicode.
        /// </summary>
        /// <param name="value">Texto que se desea codificar.</param>
        /// <returns>Objeto PDF que contiene el texto codificado.</returns>
        internal static PdfString CreateUnicodeString(string value)
        {
            string text = value ?? string.Empty;
            byte[] content = Encoding.BigEndianUnicode.GetBytes(text);
            byte[] bytes = new byte[content.Length + 2];
            bytes[0] = 0xFE;
            bytes[1] = 0xFF;
            Buffer.BlockCopy(content, 0, bytes, 2, content.Length);
            return new PdfString(bytes);
        }


        /// <summary>
        /// Convierte una cadena .NET en una cadena PDF de un solo byte.
        /// </summary>
        /// <param name="value">Texto que se desea codificar.</param>
        /// <returns>Objeto PDF que contiene el texto codificado.</returns>
        internal static PdfString CreateString(string value)
        {
            string text = value ?? string.Empty;
            return new PdfString(PdfEncoding.GetBytes(text));
        }

        /// <summary>
        /// Escapa un nombre para su escritura conforme a la sintaxis PDF.
        /// </summary>
        /// <param name="value">Nombre sin el carácter inicial '/'.</param>
        /// <returns>Nombre PDF escapado.</returns>
        private static string EscapeName(string value)
        {
            StringBuilder result = new StringBuilder();

            foreach (char character in value)
            {
                if (character <= 32 || character >= 127 ||
                    character == '#' || character == '%' || character == '/' ||
                    character == '(' || character == ')' || character == '<' ||
                    character == '>' || character == '[' || character == ']' ||
                    character == '{' || character == '}')
                {
                    result.Append('#');
                    result.Append(((int)character).ToString("X2", CultureInfo.InvariantCulture));
                }
                else
                {
                    result.Append(character);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Escribe una cadena ASCII o Latin-1 en el stream de salida.
        /// </summary>
        /// <param name="stream">Stream de destino.</param>
        /// <param name="value">Texto que se desea escribir.</param>
        private static void WriteText(Stream stream, string value)
        {
            byte[] bytes = PdfEncoding.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Escribe un objeto PDF mediante su representación sintáctica.
        /// </summary>
        /// <param name="stream">Stream de destino.</param>
        /// <param name="value">Objeto que se desea escribir.</param>
        private static void WriteObject(Stream stream, PdfObject value)
        {
            if (value == null || value is PdfNull)
            {
                WriteText(stream, "null");
                return;
            }

            if (value is PdfBoolean boolean)
            {
                WriteText(stream, boolean.Value ? "true" : "false");
                return;
            }

            if (value is PdfNumber number)
            {
                WriteText(stream, number.Value.ToString("0.################", CultureInfo.InvariantCulture));
                return;
            }

            if (value is PdfName name)
            {
                WriteText(stream, "/" + EscapeName(name.Value));
                return;
            }

            if (value is PdfString text)
            {
                WriteText(stream, "<");
                foreach (byte item in text.Bytes)
                    WriteText(stream, item.ToString("X2", CultureInfo.InvariantCulture));
                WriteText(stream, ">");
                return;
            }

            if (value is PdfReference reference)
            {
                WriteText(stream, reference.ObjectNumber.ToString(CultureInfo.InvariantCulture));
                WriteText(stream, " ");
                WriteText(stream, reference.Generation.ToString(CultureInfo.InvariantCulture));
                WriteText(stream, " R");
                return;
            }

            if (value is PdfArray array)
            {
                WriteText(stream, "[");
                for (int index = 0; index < array.Items.Count; index++)
                {
                    if (index > 0)
                        WriteText(stream, " ");
                    WriteObject(stream, array.Items[index]);
                }
                WriteText(stream, "]");
                return;
            }

            if (value is PdfDictionary dictionary)
            {
                WriteText(stream, "<<");
                foreach (KeyValuePair<string, PdfObject> item in dictionary.Items)
                {
                    WriteText(stream, "\n/");
                    WriteText(stream, EscapeName(item.Key));
                    WriteText(stream, " ");
                    WriteObject(stream, item.Value);
                }
                WriteText(stream, "\n>>");
                return;
            }

            if (value is PdfStream pdfStream)
            {
                PdfDictionary streamDictionary = pdfStream.Dictionary;
                streamDictionary.Set("Length", new PdfNumber(pdfStream.Data.Length));
                WriteObject(stream, streamDictionary);
                WriteText(stream, "\nstream\n");
                stream.Write(pdfStream.Data, 0, pdfStream.Data.Length);
                WriteText(stream, "\nendstream");
                return;
            }

            throw new NotSupportedException(
                $"No se puede escribir el tipo PDF '{value.GetType().FullName}'.");
        }

        #endregion

        #region Métodos Privados de Instancia

        /// <summary>
        /// Crea el trailer de la nueva revisión incremental.
        /// </summary>
        /// <returns>Trailer que referencia la revisión anterior.</returns>
        private PdfDictionary CreateTrailer()
        {
            PdfDictionary result = new PdfDictionary();

            foreach (KeyValuePair<string, PdfObject> item in _trailer.Items)
            {
                if (string.Equals(item.Key, "Prev", StringComparison.Ordinal) ||
                    string.Equals(item.Key, "XRefStm", StringComparison.Ordinal) ||
                    string.Equals(item.Key, "Size", StringComparison.Ordinal))
                    continue;

                result.Set(item.Key, item.Value);
            }

            foreach (KeyValuePair<string, PdfObject> item in _trailerValues)
                result.Set(item.Key, item.Value);

            int originalSize = (_trailer.Get("Size") as PdfNumber)?.ToInt32() ?? 0;
            int highestObject = _objects.Count == 0 ? 0 : _objects.Keys.Max();
            result.Set("Size", new PdfNumber(Math.Max(originalSize, highestObject + 1)));
            result.Set("Prev", new PdfNumber(_previousXref));
            return result;
        }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Establece un valor que se escribirá en el trailer de la nueva revisión.
        /// </summary>
        /// <param name="key">Clave del trailer.</param>
        /// <param name="value">Valor asociado.</param>
        internal void SetTrailerValue(string key, PdfObject value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Debe indicar la clave del trailer.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _trailerValues[key] = value;
        }

        /// <summary>
        /// Añade o sustituye un objeto indirecto en la nueva revisión.
        /// </summary>
        /// <param name="reference">Referencia indirecta del objeto.</param>
        /// <param name="value">Nuevo contenido del objeto.</param>
        internal void SetObject(PdfReference reference, PdfObject value)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _objects[reference.ObjectNumber] =
                new PdfIndirectObject(reference.ObjectNumber, reference.Generation, value);
        }

        /// <summary>
        /// Genera el contenido binario del documento con una nueva revisión incremental.
        /// </summary>
        /// <returns>Documento PDF actualizado.</returns>
        internal byte[] Write()
        {
            using (MemoryStream output = new MemoryStream())
            {
                output.Write(_source, 0, _source.Length);

                if (_source.Length > 0 && _source[_source.Length - 1] != (byte)'\n' &&
                    _source[_source.Length - 1] != (byte)'\r')
                    WriteText(output, "\n");

                Dictionary<int, long> offsets = new Dictionary<int, long>();

                foreach (PdfIndirectObject item in _objects.Values)
                {
                    offsets[item.ObjectNumber] = output.Position;
                    WriteText(output, item.ObjectNumber.ToString(CultureInfo.InvariantCulture));
                    WriteText(output, " ");
                    WriteText(output, item.Generation.ToString(CultureInfo.InvariantCulture));
                    WriteText(output, " obj\n");
                    WriteObject(output, item.Value);
                    WriteText(output, "\nendobj\n");
                }

                long xrefOffset = output.Position;
                WriteText(output, "xref\n");

                foreach (PdfIndirectObject item in _objects.Values)
                {
                    WriteText(output, item.ObjectNumber.ToString(CultureInfo.InvariantCulture));
                    WriteText(output, " 1\n");
                    WriteText(output, offsets[item.ObjectNumber].ToString("0000000000", CultureInfo.InvariantCulture));
                    WriteText(output, " ");
                    WriteText(output, item.Generation.ToString("00000", CultureInfo.InvariantCulture));
                    WriteText(output, " n \n");
                }

                WriteText(output, "trailer\n");
                WriteObject(output, CreateTrailer());
                WriteText(output, "\nstartxref\n");
                WriteText(output, xrefOffset.ToString(CultureInfo.InvariantCulture));
                WriteText(output, "\n%%EOF\n");

                return output.ToArray();
            }
        }

        #endregion

    }

    /// <summary>
    /// Representa un objeto indirecto que se escribirá en una revisión PDF.
    /// </summary>
    internal sealed class PdfIndirectObject
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa un objeto indirecto PDF.
        /// </summary>
        /// <param name="objectNumber">Número del objeto.</param>
        /// <param name="generation">Número de generación.</param>
        /// <param name="value">Contenido del objeto.</param>
        internal PdfIndirectObject(int objectNumber, int generation, PdfObject value)
        {
            ObjectNumber = objectNumber;
            Generation = generation;
            Value = value;
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el número del objeto.
        /// </summary>
        internal int ObjectNumber { get; }

        /// <summary>
        /// Obtiene el número de generación.
        /// </summary>
        internal int Generation { get; }

        /// <summary>
        /// Obtiene el contenido del objeto.
        /// </summary>
        internal PdfObject Value { get; }

        #endregion

    }

}
