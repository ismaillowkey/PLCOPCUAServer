# Industrial OPC UA Server

Sistem monitoring industrial berbasis .NET Worker Service yang mengintegrasikan data dari database ke dalam *address space* OPC UA. Proyek ini menggunakan arsitektur *decoupled* yang dibagi menjadi tiga tahap utama (Step 1, 2, dan 3) untuk memastikan integritas data dan performa tinggi.

## 🚀 Arsitektur Sistem

Proyek ini terbagi menjadi tiga komponen utama yang bekerja secara sekuensial:

1.  **Step 1: Data Initialization** (`Step1_ReadMasterTagWorker`)
    * Menarik data master tag dan daftar mesin aktif dari database/mock data.
    * Membentuk "blueprint" data di memori (`GlobalData.MachineStates`).

2.  **Step 2: Data Acquisition & Simulation** (`Step2_ReadTag`)
    * Bertindak sebagai "Kuli Data" yang mensimulasikan pembacaan PLC.
    * Menggunakan `PeriodicTimer` (default 3 detik) untuk mengupdate nilai data dan `TimestampRead`.
    * Mendukung berbagai tipe data industrial melalui standar `PlcDataType` Enum.

3.  **Step 3: OPC UA Server** (`Step3_OPCUAServerWorker`)
    * Mengekspos data dari memori ke dalam protokol OPC UA.
    * Menyinkronkan nilai dan `TimestampRead` melalui fungsi `SyncValues()` agar terbaca secara realtime oleh client (seperti UaExpert).

## 🛠 Cara Mengubah Konfigurasi

### 1. Mengubah Interval Update Data
Jika ingin mempercepat atau memperlambat detak pengambilan data, buka file `Step2_ReadTag.cs` dan ubah nilai pada `PeriodicTimer`:
```csharp
// Contoh: Ubah ke 5 detik
private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(5));
```

### 2. Menambah Tipe Data Baru
Sistem menggunakan `DataTypeId` berbasis integer (Enum) untuk menghindari kesalahan tipe data string. Jika ada tipe data baru:
1.  Buka `GlobalData.cs` dan tambahkan ID baru di `PlcDataType` Enum.
2.  Update fungsi `GenerateRandomValue` di `Step2_ReadTag.cs` untuk menangani logika randomnya.
3.  Update fungsi `GetDataTypeId` dan `GetDefaultValue` di `GenericNodeManager.cs` untuk pemetaan ke tipe data OPC UA.

### 3. Menambah Mesin atau Tag
Sistem ini bersifat dinamis. Cukup tambahkan data pada simulasi database di `Step1_ReadMasterTagWorker.cs`. Saat aplikasi di-restart:
* Folder mesin baru akan otomatis muncul di OPC UA.
* Tag baru akan otomatis dibuat di bawah folder mesin yang bersangkutan.

## 🔒 Keamanan & Sertifikat (PKI)

Aplikasi ini menggunakan standar keamanan OPC UA. Saat dijalankan pertama kali, aplikasi akan membuat folder `OPC Foundation` yang berisi sertifikat *self-signed*.

**PENTING:** Folder `OPC Foundation` mengandung *private key* dan sertifikat spesifik perangkat. **Jangan di-push ke repository GitHub**. Pastikan file `.gitignore` Anda sudah mencakup:

```text
# OPC UA Certificates & PKI
[Oo]pc [Ff]oundation/
*.pfx
*.der
```

## 💻 Kebutuhan Sistem
* **Runtime:** .NET 6.0 / 8.0
* **Library:** OPCFoundation.NetStandard.Opc.Ua
* **IDE:** Antigravity / VS Code / Visual Studio
* **Client Testing:** UaExpert

---
*Developed for Industrial Automation Monitoring System*