<img width="625" height="320" alt="image" src="https://github.com/user-attachments/assets/d056fbff-e39d-4df9-bae2-365c51125983" />

# DeCA
Librería .NET y API REST de código abierto para la generación, validación y gestión del Documento Electrónico de Control Administrativo (DeCA). Diseñada para facilitar su integración en ERP, TMS y aplicaciones de gestión del transporte.

<br>

> ### La funcionalidad de DeCA está disponible ( :wink: gratis) también en línea:
>
> :globe_with_meridians: [Acceso al API REST](https://facturae.irenesolutions.com/deca/go)
> 
> Con el API REST disponemos de una herramienta de trabajo sencilla sin la complicación de preocuparnos de la gestión de un repositorio central de documentos.


<br>
<br>

# Creación de un documento DeCA en PDF

El siguiente ejemplo crea un documento **DeCA**, lo convierte a PDF utilizando la plantilla incluida en la librería y lo guarda en disco.

```csharp
using System.IO;

// Crear el documento DeCA.
Document document = new Document()
{
    // Identificación del documento.
    OwnerPartyID = "B12959755",
    DeCAID = "DECA-2026-000001",
    DocumentNumber = "DECA-2026-000001",
    Version = 0,
    Status = "DRAFT",

    // Fechas del documento.
    CreationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    ModificationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    IssueDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    TransportDate = new DateTime(2026, 8, 1),

    // Mercancía.
    GoodsDescription = "Componentes electrónicos",
    GoodsQuantity = 1000.00m,
    GoodsQuantityUnitCode = "KGM",
    GrossWeight = 500.00m,
    IsDangerousGoods = true,

    // Vehículo.
    TractorRegistrationNumber = "MHJ3322Z",
    TrailerRegistrationNumber = "BBJ3322Z",
    SecondTrailerRegistrationNumber = "AAJ3322Z",
    SpecialCirculationAuthorizationNumber = "99-99",

    // Información adicional.
    Remarks = "Documento de ejemplo generado con DeCA.",
    ModificationReason = "Creación del documento de prueba",

    // Acceso digital.
    DownloadURL = "https://github.com/mdiago/DeCA",
    QRCodeValue = "https://github.com/mdiago/DeCA",

    // Intervinientes y lugares del transporte.
    Parties = new List<Party>()
    {
        // Cargador contractual.
        new Party()
        {
            PartyRole = "CC",
            PartyID = "B12959755",
            FullName = "IRENE SOLUTIONS SL",
            TaxID = "B12959755",
            Address = "PZ ESTANY COLOMBRI, 3B",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES",
            Mail = "info@irenesolutions.com",
            Phone = "+34 964 000 000"
        },

        // Transportista efectivo.
        new Party()
        {
            PartyRole = "TE",
            PartyID = "B44531218",
            FullName = "WEFINZ SOLUTIONS SL",
            TaxID = "B44531218",
            Address = "AV CAMINO DE ONDA, 25",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES",
            Mail = "transportes@wefinz.com",
            Phone = "+34 964 111 111"
        },

        // Expedidor o cargador efectivo.
        new Party()
        {
            PartyRole = "EX",
            PartyID = "WH01",
            FullName = "ALMACÉN CENTRAL",
            Address = "POL. INDUSTRIAL CARABONA, NAVE 12",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES"
        },

        // Destinatario de la mercancía.
        new Party()
        {
            PartyRole = "DS",
            PartyID = "B12345678",
            FullName = "CLIENTE DE EJEMPLO SL",
            TaxID = "B12345678",
            Address = "C/ MAYOR, 12",
            PostalCode = "28001",
            City = "MADRID",
            Region = "MADRID",
            CountryID = "ES",
            Mail = "recepcion@cliente.es",
            Phone = "+34 915 000 000"
        },

        // Lugar de origen del transporte.
        new Party()
        {
            PartyRole = "OR",
            FullName = "MUELLE DE CARGA 3",
            Address = "POL. INDUSTRIAL CARABONA",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES"
        },

        // Lugar de destino del transporte.
        new Party()
        {
            PartyRole = "DE",
            FullName = "PLATAFORMA LOGÍSTICA MADRID SUR",
            Address = "AV. DE LA LOGÍSTICA, 45",
            PostalCode = "28906",
            City = "GETAFE",
            Region = "MADRID",
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

- Información de identificación y control del documento.
- Cargador contractual.
- Transportista efectivo.
- Expedidor o cargador efectivo.
- Destinatario de la mercancía.
- Lugar de origen del transporte.
- Lugar de destino del transporte.
- Información de la mercancía.
- Datos del vehículo.
- Observaciones y modificaciones.
- Código QR y datos para la descarga del documento.
