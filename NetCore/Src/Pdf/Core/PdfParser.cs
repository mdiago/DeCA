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

namespace DeCA.Pdf.Core
{
    /// <summary>
    /// Parser PDF mínimo orientado a documentos controlados con AcroForm.
    /// Soporta tablas xref clásicas y objetos indirectos no contenidos en object streams.
    /// </summary>
    internal sealed class PdfParser
    {
        #region Variables Privadas de Instancia

        /// <summary>
        /// Contenido binario completo del documento PDF.
        /// </summary>
        private readonly byte[] _data;

        /// <summary>
        /// Posiciones de los objetos indirectos registradas en las tablas xref.
        /// </summary>
        private readonly Dictionary<int, long> _offsets = new Dictionary<int, long>();

        /// <summary>
        /// Objetos indirectos ya analizados y conservados en memoria.
        /// </summary>
        private readonly Dictionary<int, PdfObject> _cache = new Dictionary<int, PdfObject>();

        /// <summary>
        /// Posición actual de lectura dentro del contenido binario.
        /// </summary>
        private int _position;

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa el analizador y carga las tablas de referencias cruzadas.
        /// </summary>
        /// <param name="data">Contenido binario del documento PDF.</param>
        internal PdfParser(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            ReadCrossReferences();
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el trailer correspondiente a la revisión más reciente del PDF.
        /// </summary>
        internal PdfDictionary Trailer { get; private set; }

        /// <summary>
        /// Obtiene la posición de la tabla de referencias cruzadas correspondiente
        /// a la revisión más reciente del documento.
        /// </summary>
        internal long StartXref { get; private set; }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Resuelve sucesivamente las referencias indirectas de un objeto PDF.
        /// </summary>
        /// <param name="value">Objeto o referencia que se desea resolver.</param>
        /// <returns>Objeto PDF resuelto.</returns>
        internal PdfObject Resolve(PdfObject value)
        {
            while (value is PdfReference reference)
                value = ReadIndirectObject(reference.ObjectNumber);
            return value;
        }

        /// <summary>
        /// Resuelve un objeto como diccionario PDF.
        /// </summary>
        /// <param name="value">Objeto que se desea resolver.</param>
        /// <returns>Diccionario resuelto o null.</returns>
        internal PdfDictionary ResolveDictionary(PdfObject value)
        {
            value = Resolve(value);
            if (value is PdfDictionary dictionary)
                return dictionary;
            if (value is PdfStream stream)
                return stream.Dictionary;
            return null;
        }

        /// <summary>
        /// Resuelve un objeto como array PDF.
        /// </summary>
        /// <param name="value">Objeto que se desea resolver.</param>
        /// <returns>Array resuelto o null.</returns>
        internal PdfArray ResolveArray(PdfObject value) => Resolve(value) as PdfArray;

        /// <summary>
        /// Resuelve un objeto y obtiene su contenido textual.
        /// </summary>
        /// <param name="value">Objeto que se desea resolver.</param>
        /// <returns>Contenido textual o null.</returns>
        internal string ResolveText(PdfObject value)
        {
            value = Resolve(value);
            if (value is PdfString text)
                return text.GetText();
            if (value is PdfName name)
                return name.Value;
            return null;
        }

        /// <summary>
        /// Resuelve un objeto y obtiene su valor entero.
        /// </summary>
        /// <param name="value">Objeto que se desea resolver.</param>
        /// <returns>Valor entero o null.</returns>
        internal int? ResolveInteger(PdfObject value)
        {
            value = Resolve(value);
            return value is PdfNumber number ? number.ToInt32() : (int?)null;
        }

        #endregion

        #region Métodos Privados de Instancia

        /// <summary>
        /// Lee la cadena de tablas de referencias cruzadas y obtiene el trailer más reciente.
        /// </summary>
        private void ReadCrossReferences()
        {
            int marker = LastIndexOfAscii("startxref");
            if (marker < 0)
                throw new InvalidDataException("El PDF no contiene startxref.");

            _position = marker + "startxref".Length;
            SkipWhiteSpaceAndComments();
            long xrefOffset = ReadIntegerToken();
            StartXref = xrefOffset;

            HashSet<long> visited = new HashSet<long>();
            PdfDictionary newestTrailer = null;

            while (xrefOffset > 0 && visited.Add(xrefOffset))
            {
                _position = checked((int)xrefOffset);
                SkipWhiteSpaceAndComments();

                string token = ReadToken();
                if (!string.Equals(token, "xref", StringComparison.Ordinal))
                    throw new NotSupportedException(
                        "Esta primera versión admite tablas xref clásicas, no xref streams.");

                ReadXrefSections();
                ExpectToken("trailer");
                PdfDictionary trailer = ParseObject() as PdfDictionary;
                if (trailer == null)
                    throw new InvalidDataException("Trailer PDF no válido.");

                if (newestTrailer == null)
                    newestTrailer = trailer;

                int? previous = ResolveInteger(trailer.Get("Prev"));
                xrefOffset = previous.GetValueOrDefault(0);
            }

            Trailer = newestTrailer ?? throw new InvalidDataException("No se encontró el trailer PDF.");
        }

        /// <summary>
        /// Lee las subsecciones de la tabla de referencias cruzadas.
        /// </summary>
        private void ReadXrefSections()
        {
            while (true)
            {
                SkipWhiteSpaceAndComments();
                if (PeekAscii("trailer"))
                    return;

                int firstObject = checked((int)ReadIntegerToken());
                SkipWhiteSpaceAndComments();
                int count = checked((int)ReadIntegerToken());
                ConsumeLineEnd();

                for (int i = 0; i < count; i++)
                {
                    string line = ReadLine();
                    if (line.Length < 17)
                        throw new InvalidDataException("Entrada xref no válida.");

                    string offsetText = line.Substring(0, Math.Min(10, line.Length)).Trim();
                    char state = line.Length > 17 ? line[17] : line[line.Length - 1];

                    if (state == 'n' && long.TryParse(offsetText, NumberStyles.None,
                        CultureInfo.InvariantCulture, out long offset))
                    {
                        // Al recorrer desde la revisión más reciente hacia atrás no
                        // debemos sobrescribir una entrada ya conocida.
                        int objectNumber = firstObject + i;
                        if (!_offsets.ContainsKey(objectNumber))
                            _offsets.Add(objectNumber, offset);
                    }
                }
            }
        }

        /// <summary>
        /// Lee y analiza un objeto indirecto a partir de su número.
        /// </summary>
        private PdfObject ReadIndirectObject(int objectNumber)
        {
            if (_cache.TryGetValue(objectNumber, out PdfObject cached))
                return cached;

            if (!_offsets.TryGetValue(objectNumber, out long offset))
                throw new InvalidDataException($"No se encontró el objeto PDF {objectNumber}.");

            int oldPosition = _position;
            try
            {
                _position = checked((int)offset);
                SkipWhiteSpaceAndComments();
                int actualObject = checked((int)ReadIntegerToken());
                SkipWhiteSpaceAndComments();
                ReadIntegerToken(); // generación
                SkipWhiteSpaceAndComments();
                ExpectToken("obj");

                if (actualObject != objectNumber)
                    throw new InvalidDataException($"La entrada xref de {objectNumber} apunta a {actualObject}.");

                PdfObject value = ParseObject();
                SkipWhiteSpaceAndComments();

                if (value is PdfDictionary dictionary && PeekAscii("stream"))
                {
                    ExpectToken("stream");
                    ConsumeLineEnd();
                    int length = ResolveInteger(dictionary.Get("Length"))
                        ?? throw new NotSupportedException("Stream PDF sin /Length numérico.");
                    EnsureAvailable(length);
                    byte[] bytes = new byte[length];
                    Buffer.BlockCopy(_data, _position, bytes, 0, length);
                    _position += length;
                    value = new PdfStream(dictionary, bytes);
                }

                _cache[objectNumber] = value;
                return value;
            }
            finally
            {
                _position = oldPosition;
            }
        }

        /// <summary>
        /// Analiza el objeto PDF situado en la posición actual.
        /// </summary>
        private PdfObject ParseObject()
        {
            SkipWhiteSpaceAndComments();
            if (_position >= _data.Length)
                throw new EndOfStreamException();

            byte current = _data[_position];
            if (current == (byte)'<')
            {
                if (_position + 1 < _data.Length && _data[_position + 1] == (byte)'<')
                    return ParseDictionary();
                return ParseHexString();
            }
            if (current == (byte)'[') return ParseArray();
            if (current == (byte)'(') return ParseLiteralString();
            if (current == (byte)'/') return ParseName();

            string token = ReadToken();
            if (token == "true") return new PdfBoolean(true);
            if (token == "false") return new PdfBoolean(false);
            if (token == "null") return PdfNull.Value;

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                int savedPosition = _position;
                SkipWhiteSpaceAndComments();

                if (_position < _data.Length && IsNumberStart(_data[_position]))
                {
                    string secondToken = ReadToken();

                    if (int.TryParse(token, NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int objectNumber) &&
                        int.TryParse(secondToken, NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int generationNumber))
                    {
                        SkipWhiteSpaceAndComments();

                        if (_position < _data.Length && !IsDelimiter(_data[_position]))
                        {
                            string thirdToken = ReadToken();
                            if (string.Equals(thirdToken, "R", StringComparison.Ordinal))
                                return new PdfReference(objectNumber, generationNumber);
                        }
                    }
                }

                _position = savedPosition;
                return new PdfNumber(number);
            }

            throw new InvalidDataException($"Objeto PDF no reconocido: '{token}'.");
        }

        /// <summary>
        /// Analiza un diccionario PDF.
        /// </summary>
        private PdfDictionary ParseDictionary()
        {
            _position += 2; // <<
            var dictionary = new PdfDictionary();
            while (true)
            {
                SkipWhiteSpaceAndComments();
                if (PeekAscii(">>"))
                {
                    _position += 2;
                    return dictionary;
                }

                PdfName key = ParseName();
                PdfObject value = ParseObject();
                dictionary.Set(key.Value, value);
            }
        }

        /// <summary>
        /// Analiza un array PDF.
        /// </summary>
        private PdfArray ParseArray()
        {
            _position++; // [
            var array = new PdfArray();
            while (true)
            {
                SkipWhiteSpaceAndComments();
                if (_position >= _data.Length)
                    throw new EndOfStreamException();
                if (_data[_position] == (byte)']')
                {
                    _position++;
                    return array;
                }
                array.Items.Add(ParseObject());
            }
        }

        /// <summary>
        /// Analiza un nombre PDF.
        /// </summary>
        private PdfName ParseName()
        {
            if (_data[_position] != (byte)'/')
                throw new InvalidDataException("Se esperaba un nombre PDF.");
            _position++;
            var bytes = new List<byte>();
            while (_position < _data.Length && !IsWhiteSpace(_data[_position]) && !IsDelimiter(_data[_position]))
            {
                byte value = _data[_position++];
                if (value == (byte)'#' && _position + 1 < _data.Length &&
                    TryHex(_data[_position], out int high) && TryHex(_data[_position + 1], out int low))
                {
                    bytes.Add((byte)((high << 4) | low));
                    _position += 2;
                }
                else
                {
                    bytes.Add(value);
                }
            }
            return new PdfName(Encoding.ASCII.GetString(bytes.ToArray()));
        }

        /// <summary>
        /// Analiza una cadena literal PDF.
        /// </summary>
        private PdfString ParseLiteralString()
        {
            _position++; // (
            int level = 1;
            var bytes = new List<byte>();
            while (_position < _data.Length && level > 0)
            {
                byte value = _data[_position++];
                if (value == (byte)'\\')
                {
                    if (_position >= _data.Length) break;
                    byte escaped = _data[_position++];
                    switch (escaped)
                    {
                        case (byte)'n': bytes.Add((byte)'\n'); break;
                        case (byte)'r': bytes.Add((byte)'\r'); break;
                        case (byte)'t': bytes.Add((byte)'\t'); break;
                        case (byte)'b': bytes.Add((byte)'\b'); break;
                        case (byte)'f': bytes.Add((byte)'\f'); break;
                        case (byte)'\r':
                            if (_position < _data.Length && _data[_position] == (byte)'\n') _position++;
                            break;
                        case (byte)'\n': break;
                        default:
                            if (escaped >= (byte)'0' && escaped <= (byte)'7')
                            {
                                int octal = escaped - (byte)'0';
                                int count = 1;
                                while (count < 3 && _position < _data.Length &&
                                    _data[_position] >= (byte)'0' && _data[_position] <= (byte)'7')
                                {
                                    octal = octal * 8 + (_data[_position++] - (byte)'0');
                                    count++;
                                }
                                bytes.Add((byte)octal);
                            }
                            else bytes.Add(escaped);
                            break;
                    }
                }
                else if (value == (byte)'(')
                {
                    level++;
                    bytes.Add(value);
                }
                else if (value == (byte)')')
                {
                    level--;
                    if (level > 0) bytes.Add(value);
                }
                else bytes.Add(value);
            }
            if (level != 0) throw new InvalidDataException("String PDF sin cerrar.");
            return new PdfString(bytes.ToArray());
        }

        /// <summary>
        /// Analiza una cadena hexadecimal PDF.
        /// </summary>
        private PdfString ParseHexString()
        {
            _position++; // <
            var nibbles = new List<int>();
            while (_position < _data.Length && _data[_position] != (byte)'>')
            {
                byte value = _data[_position++];
                if (IsWhiteSpace(value)) continue;
                if (!TryHex(value, out int nibble))
                    throw new InvalidDataException("String hexadecimal PDF no válido.");
                nibbles.Add(nibble);
            }
            if (_position >= _data.Length) throw new InvalidDataException("String hexadecimal sin cerrar.");
            _position++;
            if ((nibbles.Count & 1) != 0) nibbles.Add(0);
            byte[] bytes = new byte[nibbles.Count / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)((nibbles[i * 2] << 4) | nibbles[i * 2 + 1]);
            return new PdfString(bytes);
        }

        /// <summary>
        /// Lee un token y lo convierte en un valor entero.
        /// </summary>
        private long ReadIntegerToken()
        {
            string token = ReadToken();
            if (!long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                throw new InvalidDataException($"Se esperaba un entero PDF y se encontró '{token}'.");
            return value;
        }

        /// <summary>
        /// Lee el siguiente token del documento.
        /// </summary>
        private string ReadToken()
        {
            SkipWhiteSpaceAndComments();
            int start = _position;
            while (_position < _data.Length && !IsWhiteSpace(_data[_position]) && !IsDelimiter(_data[_position]))
                _position++;
            if (start == _position)
                throw new InvalidDataException($"Token PDF inesperado en la posición {_position}.");
            return Encoding.ASCII.GetString(_data, start, _position - start);
        }

        /// <summary>
        /// Comprueba que el siguiente token coincide con el valor esperado.
        /// </summary>
        private void ExpectToken(string expected)
        {
            string actual = ReadToken();
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException($"Se esperaba '{expected}' y se encontró '{actual}'.");
        }

        /// <summary>
        /// Avanza sobre espacios en blanco y comentarios.
        /// </summary>
        private void SkipWhiteSpaceAndComments()
        {
            while (_position < _data.Length)
            {
                if (IsWhiteSpace(_data[_position])) { _position++; continue; }
                if (_data[_position] == (byte)'%')
                {
                    while (_position < _data.Length && _data[_position] != (byte)'\r' && _data[_position] != (byte)'\n')
                        _position++;
                    continue;
                }
                break;
            }
        }

        /// <summary>
        /// Consume el final de línea situado en la posición actual.
        /// </summary>
        private void ConsumeLineEnd()
        {
            if (_position < _data.Length && _data[_position] == (byte)'\r') _position++;
            if (_position < _data.Length && _data[_position] == (byte)'\n') _position++;
        }

        /// <summary>
        /// Lee el contenido hasta el siguiente final de línea.
        /// </summary>
        private string ReadLine()
        {
            int start = _position;
            while (_position < _data.Length && _data[_position] != (byte)'\r' && _data[_position] != (byte)'\n')
                _position++;
            string result = Encoding.ASCII.GetString(_data, start, _position - start);
            ConsumeLineEnd();
            return result;
        }

        /// <summary>
        /// Comprueba si el texto ASCII indicado aparece en la posición actual.
        /// </summary>
        private bool PeekAscii(string text)
        {
            if (_position + text.Length > _data.Length) return false;
            for (int i = 0; i < text.Length; i++)
                if (_data[_position + i] != (byte)text[i]) return false;
            return true;
        }

        /// <summary>
        /// Busca la última aparición de un texto ASCII en el documento.
        /// </summary>
        private int LastIndexOfAscii(string text)
        {
            byte[] needle = Encoding.ASCII.GetBytes(text);
            for (int i = _data.Length - needle.Length; i >= 0; i--)
            {
                bool matches = true;
                for (int j = 0; j < needle.Length; j++)
                    if (_data[i + j] != needle[j]) { matches = false; break; }
                if (matches) return i;
            }
            return -1;
        }

        /// <summary>
        /// Comprueba que quedan suficientes bytes disponibles para la lectura.
        /// </summary>
        private void EnsureAvailable(int count)
        {
            if (count < 0 || _position + count > _data.Length)
                throw new EndOfStreamException();
        }

        #endregion

        #region Métodos Privados Estáticos

        /// <summary>
        /// Intenta convertir un carácter hexadecimal en su valor numérico.
        /// </summary>
        /// <param name="value">Carácter que se desea convertir.</param>
        /// <param name="result">Valor hexadecimal obtenido.</param>
        /// <returns>Verdadero cuando el carácter es hexadecimal.</returns>
        private static bool TryHex(byte value, out int result)
        {
            if (value >= '0' && value <= '9') { result = value - '0'; return true; }
            if (value >= 'A' && value <= 'F') { result = value - 'A' + 10; return true; }
            if (value >= 'a' && value <= 'f') { result = value - 'a' + 10; return true; }
            result = 0;
            return false;
        }

        /// <summary>
        /// Indica si un byte representa un espacio en blanco PDF.
        /// </summary>
        /// <param name="value">Byte que se desea comprobar.</param>
        /// <returns>Verdadero cuando el byte es un espacio en blanco.</returns>
        private static bool IsWhiteSpace(byte value) =>
            value == 0 || value == 9 || value == 10 || value == 12 || value == 13 || value == 32;

        /// <summary>
        /// Indica si un byte representa un delimitador PDF.
        /// </summary>
        /// <param name="value">Byte que se desea comprobar.</param>
        /// <returns>Verdadero cuando el byte es un delimitador.</returns>
        /// <summary>
        /// Indica si un byte puede representar el primer carácter de un número PDF.
        /// </summary>
        /// <param name="value">Byte que se desea comprobar.</param>
        /// <returns>Verdadero cuando el byte puede iniciar un número PDF.</returns>
        private static bool IsNumberStart(byte value)
        {
            return value == (byte)'+' ||
                value == (byte)'-' ||
                value == (byte)'.' ||
                (value >= (byte)'0' && value <= (byte)'9');
        }

        private static bool IsDelimiter(byte value) =>
            value == '(' || value == ')' || value == '<' || value == '>' || value == '[' || value == ']' ||
            value == '{' || value == '}' || value == '/' || value == '%';
        #endregion

    }
}
