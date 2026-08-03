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
using DeCA.Config;
using System.Globalization;
using System.Text;

namespace DeCA.Business
{

    /// <summary>
    /// Gestiona un contador incremental y persistente para la identificación
    /// de documentos DeCA definitivos.
    /// </summary>
    /// <remarks>
    /// Existe una única instancia del contador para cada combinación de
    /// propietario del documento y ejercicio.
    /// </remarks>
    public sealed class DeCADocumentCounter :
        SingletonByKey<DeCADocumentCounter>
    {

        #region Variables Privadas Estáticas

        /// <summary>
        /// Carácter utilizado para separar el identificador del propietario
        /// y el ejercicio dentro de la clave del contador.
        /// </summary>
        private const char _KeySeparator = '.';

        #endregion

        #region Variables Privadas de Instancia

        /// <summary>
        /// Objeto utilizado para sincronizar las operaciones realizadas
        /// sobre el contador.
        /// </summary>
        private readonly object _Locker;

        /// <summary>
        /// Identificador del interlocutor propietario de los documentos.
        /// </summary>
        private readonly string _OwnerPartyID;

        /// <summary>
        /// Ejercicio al que corresponde el contador.
        /// </summary>
        private readonly int _Year;

        /// <summary>
        /// Último número reservado y persistido por el contador.
        /// </summary>
        private int _CurrentValue;

        #endregion

        #region Propiedades Privadas de Instancia

        /// <summary>
        /// Obtiene la ruta completa del fichero utilizado para persistir
        /// el valor del contador.
        /// </summary>
        private string CounterFilePath
        {
            get
            {
                return Path.Combine(
                    Settings.Current.CountPath,
                    Key);
            }
        }

        /// <summary>
        /// Obtiene la ruta del fichero temporal utilizado durante
        /// la actualización atómica del contador.
        /// </summary>
        private string TemporaryFilePath
        {
            get
            {
                return CounterFilePath + ".tmp";
            }
        }

        /// <summary>
        /// Obtiene la ruta del fichero de respaldo utilizado durante
        /// la sustitución del contador.
        /// </summary>
        private string BackupFilePath
        {
            get
            {
                return CounterFilePath + ".bak";
            }
        }

        #endregion

        #region Constructores de Instancia

        /// <summary>
        /// Inicializa una nueva instancia del contador a partir de su clave.
        /// </summary>
        /// <param name="key">
        /// Clave compuesta por el identificador del propietario del documento
        /// y el ejercicio, separados mediante un punto.
        /// </param>
        /// <exception cref="ArgumentException">
        /// La clave no tiene un formato válido.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// No se ha configurado el directorio de almacenamiento de contadores.
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// El fichero persistido no contiene un contador válido.
        /// </exception>
        public DeCADocumentCounter(string key)
            : base(key)
        {
            _Locker = new object();

            ParseKey(
                Key,
                out _OwnerPartyID,
                out _Year);

            ValidateCountPath();

            Directory.CreateDirectory(
                Settings.Current.CountPath);

            DeleteTemporaryFile();

            _CurrentValue = LoadCurrentValue();
        }

        #endregion

        #region Métodos Privados Estáticos

        /// <summary>
        /// Crea la clave que identifica una instancia del contador.
        /// </summary>
        /// <param name="ownerPartyID">
        /// Identificador del interlocutor propietario de los documentos.
        /// </param>
        /// <param name="year">
        /// Ejercicio al que corresponde el contador.
        /// </param>
        /// <returns>
        /// Clave formada por el identificador del propietario y el ejercicio,
        /// separados mediante un punto.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// El identificador del propietario está vacío, contiene el separador
        /// reservado o no puede utilizarse como nombre de fichero.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// El ejercicio indicado no es válido.
        /// </exception>
        private static string CreateKey(
            string ownerPartyID,
            int year)
        {
            if (string.IsNullOrWhiteSpace(ownerPartyID))
            {
                throw new ArgumentException(
                    "Debe indicar el identificador del propietario del documento.",
                    nameof(ownerPartyID));
            }

            if (year < 1 ||
                year > 9999)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "El ejercicio indicado no es válido.");
            }

            string normalizedOwnerPartyID =
                ownerPartyID.Trim().ToUpperInvariant();

            if (normalizedOwnerPartyID.IndexOf(_KeySeparator) >= 0)
            {
                throw new ArgumentException(
                    $"El identificador del propietario no puede contener " +
                    $"el carácter '{_KeySeparator}'.",
                    nameof(ownerPartyID));
            }

            if (normalizedOwnerPartyID.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException(
                    "El identificador del propietario contiene caracteres " +
                    "que no pueden utilizarse en el nombre del fichero del contador.",
                    nameof(ownerPartyID));
            }

            return string.Concat(
                normalizedOwnerPartyID,
                _KeySeparator.ToString(),
                year.ToString(
                    "0000",
                    CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Obtiene los componentes contenidos en la clave del contador.
        /// </summary>
        /// <param name="key">
        /// Clave que se desea interpretar.
        /// </param>
        /// <param name="ownerPartyID">
        /// Identificador del propietario obtenido de la clave.
        /// </param>
        /// <param name="year">
        /// Ejercicio obtenido de la clave.
        /// </param>
        /// <exception cref="ArgumentException">
        /// La clave no tiene el formato esperado.
        /// </exception>
        private static void ParseKey(
            string key,
            out string ownerPartyID,
            out int year)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "La clave del contador no puede estar vacía.",
                    nameof(key));
            }

            int separatorPosition =
                key.LastIndexOf(_KeySeparator);

            if (separatorPosition <= 0 ||
                separatorPosition >= key.Length - 1)
            {
                throw new ArgumentException(
                    $"La clave '{key}' no tiene un formato válido.",
                    nameof(key));
            }

            ownerPartyID =
                key.Substring(
                    0,
                    separatorPosition);

            string yearValue =
                key.Substring(
                    separatorPosition + 1);

            if (!int.TryParse(
                yearValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out year))
            {
                throw new ArgumentException(
                    $"La clave '{key}' no contiene un ejercicio válido.",
                    nameof(key));
            }

            if (year < 1 ||
                year > 9999)
            {
                throw new ArgumentException(
                    $"La clave '{key}' contiene un ejercicio fuera del rango válido.",
                    nameof(key));
            }
        }

        #endregion

        #region Métodos Privados de Instancia

        /// <summary>
        /// Comprueba que el directorio de almacenamiento de contadores
        /// se encuentra correctamente configurado.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// El directorio de almacenamiento no está configurado.
        /// </exception>
        private void ValidateCountPath()
        {
            if (string.IsNullOrWhiteSpace(
                Settings.Current.CountPath))
            {
                throw new InvalidOperationException(
                    "No se ha configurado el directorio de almacenamiento " +
                    "de los contadores DeCA.");
            }
        }

        /// <summary>
        /// Recupera de disco el último valor persistido del contador.
        /// </summary>
        /// <returns>
        /// Último valor reservado o cero cuando todavía no existe
        /// un fichero para el contador.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// El fichero existe, pero está vacío, contiene un valor inválido
        /// o contiene un número negativo.
        /// </exception>
        private int LoadCurrentValue()
        {
            if (!File.Exists(CounterFilePath))
            {
                return 0;
            }

            string content =
                File.ReadAllText(
                    CounterFilePath,
                    Encoding.UTF8).Trim();

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidDataException(
                    $"El fichero del contador '{CounterFilePath}' está vacío.");
            }

            if (!int.TryParse(
                content,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int currentValue))
            {
                throw new InvalidDataException(
                    $"El fichero del contador '{CounterFilePath}' " +
                    "no contiene un número válido.");
            }

            if (currentValue < 0)
            {
                throw new InvalidDataException(
                    $"El fichero del contador '{CounterFilePath}' " +
                    "contiene un valor negativo.");
            }

            return currentValue;
        }

        /// <summary>
        /// Persiste de forma atómica el valor indicado para el contador.
        /// </summary>
        /// <param name="value">
        /// Valor que se desea conservar.
        /// </param>
        /// <remarks>
        /// El valor se escribe inicialmente en un fichero temporal.
        /// Cuando la escritura ha terminado y se ha forzado su persistencia,
        /// el fichero temporal sustituye al fichero definitivo.
        /// </remarks>
        private void SaveCurrentValue(int value)
        {
            Directory.CreateDirectory(
                Settings.Current.CountPath);

            byte[] content =
                Encoding.UTF8.GetBytes(
                    value.ToString(
                        CultureInfo.InvariantCulture));

            using (FileStream stream = new FileStream(
                TemporaryFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.WriteThrough))
            {
                stream.Write(
                    content,
                    0,
                    content.Length);

                stream.Flush(true);
            }

            ReplaceCounterFile();
        }

        /// <summary>
        /// Sustituye el fichero definitivo del contador por el fichero
        /// temporal previamente escrito.
        /// </summary>
        private void ReplaceCounterFile()
        {
            if (File.Exists(CounterFilePath))
            {
                DeleteBackupFile();

                File.Replace(
                    TemporaryFilePath,
                    CounterFilePath,
                    BackupFilePath,
                    true);

                DeleteBackupFile();
            }
            else
            {
                File.Move(
                    TemporaryFilePath,
                    CounterFilePath);
            }
        }

        /// <summary>
        /// Elimina el fichero temporal cuando ha quedado pendiente
        /// de una ejecución anterior.
        /// </summary>
        private void DeleteTemporaryFile()
        {
            if (File.Exists(TemporaryFilePath))
            {
                File.Delete(TemporaryFilePath);
            }
        }

        /// <summary>
        /// Elimina el fichero de respaldo cuando existe.
        /// </summary>
        private void DeleteBackupFile()
        {
            if (File.Exists(BackupFilePath))
            {
                File.Delete(BackupFilePath);
            }
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Obtiene el identificador del interlocutor propietario
        /// de los documentos.
        /// </summary>
        public string OwnerPartyID
        {
            get
            {
                return _OwnerPartyID;
            }
        }

        /// <summary>
        /// Obtiene el ejercicio al que corresponde el contador.
        /// </summary>
        public int Year
        {
            get
            {
                return _Year;
            }
        }

        /// <summary>
        /// Obtiene el último número reservado y persistido.
        /// </summary>
        public int CurrentValue
        {
            get
            {
                lock (_Locker)
                {
                    return _CurrentValue;
                }
            }
        }

        /// <summary>
        /// Obtiene la ruta del fichero utilizado para conservar
        /// el valor del contador.
        /// </summary>
        public string FilePath
        {
            get
            {
                return CounterFilePath;
            }
        }

        #endregion

        #region Métodos Públicos Estáticos

        /// <summary>
        /// Obtiene el contador correspondiente a un propietario
        /// y una fecha de emisión.
        /// </summary>
        /// <param name="ownerPartyID">
        /// Identificador del interlocutor propietario del documento.
        /// </param>
        /// <param name="issueDateTime">
        /// Fecha de emisión definitiva del documento.
        /// </param>
        /// <returns>
        /// Contador correspondiente al propietario y ejercicio indicados.
        /// </returns>
        public static DeCADocumentCounter Get(
            string ownerPartyID,
            DateTime issueDateTime)
        {
            return Get(
                ownerPartyID,
                issueDateTime.Year);
        }

        /// <summary>
        /// Obtiene el contador correspondiente a un propietario
        /// y un ejercicio.
        /// </summary>
        /// <param name="ownerPartyID">
        /// Identificador del interlocutor propietario del documento.
        /// </param>
        /// <param name="year">
        /// Ejercicio del contador.
        /// </param>
        /// <returns>
        /// Contador correspondiente al propietario y ejercicio indicados.
        /// </returns>
        public static DeCADocumentCounter Get(
            string ownerPartyID,
            int year)
        {
            string key =
                CreateKey(
                    ownerPartyID,
                    year);

            return GetInstance(key) as DeCADocumentCounter;
        }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Reserva, persiste y devuelve el siguiente número disponible.
        /// </summary>
        /// <returns>
        /// Siguiente número reservado por el contador.
        /// </returns>
        /// <exception cref="OverflowException">
        /// El contador ha alcanzado el valor máximo admitido por
        /// un entero de 32 bits.
        /// </exception>
        /// <exception cref="IOException">
        /// No se ha podido persistir el nuevo valor del contador.
        /// </exception>
        /// <remarks>
        /// El número se considera consumido desde el momento en el que
        /// este método lo devuelve. Si posteriormente falla la generación
        /// del documento, el número no se reutiliza.
        /// </remarks>
        public int Next()
        {
            lock (_Locker)
            {
                int nextValue =
                    checked(_CurrentValue + 1);

                SaveCurrentValue(nextValue);

                _CurrentValue = nextValue;

                return _CurrentValue;
            }
        }

        #endregion

    }

}