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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DECa.Pdf.Core
{

    /// <summary>
    /// Clase base para todos los objetos que forman parte de la estructura interna de un PDF.
    /// </summary>
    internal abstract class PdfObject
    {
    }

    /// <summary>
    /// Representa el valor nulo de un documento PDF.
    /// </summary>
    internal sealed class PdfNull : PdfObject
    {

        #region Variables Privadas Estáticas

        /// <summary>
        /// Instancia única que representa el valor nulo PDF.
        /// </summary>
        internal static readonly PdfNull Value = new PdfNull();

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa la instancia única del valor nulo PDF.
        /// </summary>
        private PdfNull()
        {
        }

        #endregion

    }

    /// <summary>
    /// Representa un valor lógico de un documento PDF.
    /// </summary>
    internal sealed class PdfBoolean : PdfObject
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa un valor lógico PDF.
        /// </summary>
        /// <param name="value">Valor lógico.</param>
        internal PdfBoolean(bool value)
        {
            Value = value;
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el valor lógico almacenado.
        /// </summary>
        internal bool Value { get; }

        #endregion

    }

    /// <summary>
    /// Representa un valor numérico de un documento PDF.
    /// </summary>
    internal sealed class PdfNumber : PdfObject
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa un valor numérico PDF.
        /// </summary>
        /// <param name="value">Valor numérico.</param>
        internal PdfNumber(double value)
        {
            Value = value;
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el valor numérico almacenado.
        /// </summary>
        internal double Value { get; }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Convierte el valor numérico a entero de 32 bits.
        /// </summary>
        /// <returns>Valor convertido a entero.</returns>
        internal int ToInt32() => checked((int)Value);

        /// <summary>
        /// Devuelve la representación textual del número usando la cultura invariante.
        /// </summary>
        /// <returns>Representación textual del valor.</returns>
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        #endregion

    }

    /// <summary>
    /// Representa un nombre de un documento PDF.
    /// </summary>
    internal sealed class PdfName : PdfObject
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa un nombre PDF.
        /// </summary>
        /// <param name="value">Valor del nombre sin el carácter inicial '/'.</param>
        internal PdfName(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el valor del nombre PDF.
        /// </summary>
        internal string Value { get; }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Devuelve la representación textual del nombre PDF.
        /// </summary>
        /// <returns>Nombre precedido por el carácter '/'.</returns>
        public override string ToString() => "/" + Value;

        #endregion

    }

    /// <summary>
    /// Representa una cadena de bytes de un documento PDF.
    /// </summary>
    internal sealed class PdfString : PdfObject
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa una cadena PDF.
        /// </summary>
        /// <param name="bytes">Contenido binario de la cadena.</param>
        internal PdfString(byte[] bytes)
        {
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el contenido binario de la cadena.
        /// </summary>
        internal byte[] Bytes { get; }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Decodifica el contenido de la cadena PDF.
        /// </summary>
        /// <returns>Texto decodificado.</returns>
        internal string GetText()
        {
            if (Bytes.Length >= 2 && Bytes[0] == 0xFE && Bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(Bytes, 2, Bytes.Length - 2);

            return Encoding.GetEncoding("ISO-8859-1").GetString(Bytes);
        }

        /// <summary>
        /// Devuelve el contenido textual de la cadena PDF.
        /// </summary>
        /// <returns>Texto decodificado.</returns>
        public override string ToString() => GetText();

        #endregion

    }

    /// <summary>
    /// Representa un array de objetos PDF.
    /// </summary>
    internal sealed class PdfArray : PdfObject
    {

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene los objetos contenidos en el array.
        /// </summary>
        internal List<PdfObject> Items { get; } = new List<PdfObject>();

        #endregion

    }

    /// <summary>
    /// Representa un diccionario de objetos PDF.
    /// </summary>
    internal sealed class PdfDictionary : PdfObject
    {

        #region Variables Privadas de Instancia

        /// <summary>
        /// Elementos almacenados en el diccionario PDF.
        /// </summary>
        private readonly Dictionary<string, PdfObject> _items =
            new Dictionary<string, PdfObject>(StringComparer.Ordinal);

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene los elementos almacenados en el diccionario PDF.
        /// </summary>
        internal IEnumerable<KeyValuePair<string, PdfObject>> Items => _items;

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Establece un elemento del diccionario.
        /// </summary>
        /// <param name="key">Clave del elemento.</param>
        /// <param name="value">Valor asociado.</param>
        internal void Set(string key, PdfObject value) => _items[key] = value;

        /// <summary>
        /// Intenta obtener un elemento del diccionario.
        /// </summary>
        /// <param name="key">Clave buscada.</param>
        /// <param name="value">Valor encontrado.</param>
        /// <returns>Verdadero cuando existe la clave.</returns>
        internal bool TryGet(string key, out PdfObject value) => _items.TryGetValue(key, out value);

        /// <summary>
        /// Obtiene un elemento del diccionario.
        /// </summary>
        /// <param name="key">Clave buscada.</param>
        /// <returns>Valor asociado o null cuando no existe.</returns>
        internal PdfObject Get(string key)
        {
            _items.TryGetValue(key, out PdfObject value);
            return value;
        }

        /// <summary>
        /// Elimina un elemento del diccionario.
        /// </summary>
        /// <param name="key">Clave del elemento que se desea eliminar.</param>
        /// <returns>Verdadero cuando el elemento existía y ha sido eliminado.</returns>
        internal bool Remove(string key) => _items.Remove(key);

        #endregion

    }

    /// <summary>
    /// Representa una referencia indirecta a otro objeto PDF.
    /// </summary>
    internal sealed class PdfReference : PdfObject
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa una referencia indirecta PDF.
        /// </summary>
        /// <param name="objectNumber">Número del objeto referenciado.</param>
        /// <param name="generation">Número de generación.</param>
        internal PdfReference(int objectNumber, int generation)
        {
            ObjectNumber = objectNumber;
            Generation = generation;
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el número del objeto referenciado.
        /// </summary>
        internal int ObjectNumber { get; }

        /// <summary>
        /// Obtiene el número de generación de la referencia.
        /// </summary>
        internal int Generation { get; }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Devuelve la representación textual de la referencia indirecta.
        /// </summary>
        /// <returns>Referencia en formato PDF.</returns>
        public override string ToString() => $"{ObjectNumber} {Generation} R";

        #endregion

    }

    /// <summary>
    /// Representa un stream de datos de un documento PDF.
    /// </summary>
    internal sealed class PdfStream : PdfObject
    {

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa un stream PDF.
        /// </summary>
        /// <param name="dictionary">Diccionario asociado al stream.</param>
        /// <param name="data">Contenido binario del stream.</param>
        internal PdfStream(PdfDictionary dictionary, byte[] data)
        {
            Dictionary = dictionary;
            Data = data;
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el diccionario asociado al stream.
        /// </summary>
        internal PdfDictionary Dictionary { get; }

        /// <summary>
        /// Obtiene el contenido binario del stream.
        /// </summary>
        internal byte[] Data { get; }

        #endregion

    }

}
