<img width="625" height="320" alt="image" src="https://github.com/user-attachments/assets/d056fbff-e39d-4df9-bae2-365c51125983" />

# DeCA
Librería .NET y API REST de código abierto para la generación, validación y gestión del Documento Electrónico de Control Administrativo (DeCA). Diseñada para facilitar su integración en ERP, TMS y aplicaciones de gestión del transporte.

## Creación de un documento DeCA en PDF

El siguiente ejemplo crea un documento **DeCA**, lo convierte a PDF utilizando la plantilla incluida en la librería y lo guarda en disco.

```csharp
using System.IO;

// Crear el documento DeCA.
Document document = new Document()
{
    // Identificación del documento.
    DeCAID = "DECA-2026-000001",
    DocumentNumber = "DECA-2026-000001",
    Version = 0,
    Status = "DRAFT",

    // Fechas del documento.
    CreationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    ModificationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    IssueDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    TransportDate = new DateTime(2026, 8, 1),

    // Información de la mercancía.
    GoodsDescription = "Componentes electrónicos",
    GoodsQuantity = 1000.00m,
    GoodsQuantityUnitCode = "KGM",
    GrossWeight = 500.00m,
    IsDangerousGoods = true,

    // Datos del vehículo.
    TractorRegistrationNumber = "MHJ3322Z",
    TrailerRegistrationNumber = "BBJ3322Z",
    SecondTrailerRegistrationNumber = "AAJ3322Z",
    SpecialCirculationAuthorizationNumber = "99-99",

    // Información adicional.
    Remarks = "Documento de ejemplo generado con DeCA.",
    ModificationReason = "Creación del documento de prueba",

    // Enlaces y código QR.
    DownloadURL = "https://github.com/mdiago/DeCA",
    QRCodeValue = "https://github.com/mdiago/DeCA",

    // Intervinientes del transporte.
    Parties = new List<Party>()
    {
        new Party()
        {
            PartyRole = "CC",
            PartyID = "B12959755",
            FullName = "IRENE SOLUTIONS SL",
            TaxID = "B12959755",
            Address = "PZ ESTANY COLOMBRI, 3B",
            PostalCode = "12530",
            City = "BURRIANA",
            CountryID = "ES",
            Mail = "info@irenesolutions.com",
            Phone = "+34 964 000 000"
        },

        new Party()
        {
            PartyRole = "TE",
            PartyID = "B44531218",
            FullName = "WEFINZ SOLUTIONS SL",
            TaxID = "B44531218",
            Address = "AV CAMINO DE ONDA, 25",
            PostalCode = "12530",
            City = "BURRIANA",
            CountryID = "ES"
        },

        new Party()
        {
            PartyRole = "DS",
            PartyID = "B12345678",
            FullName = "CLIENTE DE EJEMPLO SL",
            TaxID = "B12345678",
            Address = "C/ MAYOR, 12",
            PostalCode = "28001",
            City = "MADRID",
            CountryID = "ES"
        }
    }
};

// Generar el PDF utilizando la plantilla incluida en la librería.
DeCAPdfConverter converter = new DeCAPdfConverter(document);

// Guardar el documento PDF.
File.WriteAllBytes("DeCA.pdf", converter.GetPdf());
```

El PDF generado incluirá automáticamente:

- La información general del documento DeCA.
- La información de la mercancía.
- Los datos del vehículo.
- Los distintos intervinientes del transporte.
- El código QR.
- Campos de texto.
- Casillas de verificación (booleanos).
- Imágenes insertadas en botones del formulario PDF.
