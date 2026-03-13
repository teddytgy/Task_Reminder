# Integrations

The solution now includes disabled-by-default integration scaffolding for future office system connectivity.

## Provider categories

- `OpenDentalAppointments`
- `PatientXpressInsurance`
- `CsvManualImport`
- `PatientCommunication`

## Current behavior

- Providers are stored in the database with enabled/disabled state and notes.
- Each run writes a status record with start time, end time, status, and message.
- The current implementations are placeholders intended to preserve architecture for future real integrations.

## Future extension path

Implement one or more of these interfaces:

- `IExternalAppointmentSyncProvider`
- `IExternalInsuranceVerificationProvider`
- `IExternalPatientCommunicationProvider`

Then register the real provider in `Program.cs` and keep the existing API/WPF management flow.
