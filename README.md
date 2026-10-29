# 🚀 Antigravity Çoklu Hesap Yöneticisi (Antigravity Account Manager)

Google Antigravity ve Gemini aboneliklerinizi (Google One AI Premium, Gemini Advanced vb.) tek bir bilgisayarda kolayca yönetmenizi ve kotalar arasında kesintisiz geçiş yapmanızı sağlayan modern bir Windows masaüstü uygulamasıdır.

---

## 💡 Neden Geliştirildi?

Google Antigravity, Google oturum jetonlarını (`access_token`, `refresh_token`, `id_token`) yalnızca yerel dosyalarda değil, doğrudan **Windows Kimlik Bilgisi Yöneticisi (Windows Credential Manager)** içerisindeki `gemini:antigravity` anahtarında saklar. Bu durum, birden fazla hesap arasında geçiş yapmayı zorlaştırır.

**Antigravity Çoklu Hesap Yöneticisi**, hem Windows Credential Manager'daki anahtarları hem de Antigravity arayüz oturumlarını (`--user-data-dir`) senkronize ederek **tek tıkla, şifre ve 2FA girmeden hesaplar arasında anında geçiş yapmanızı sağlar.**

---

## ✨ Özellikler

* **Sınırsız Hesap Desteği:** Dilediğiniz kadar Google / Gemini hesabını listeye ekleyebilirsiniz.
* **Sıfır Form ile Hesap Ekleme Sihirbazı:** 
  * Elle e-posta veya şifre yazma formu yoktur.
  * *"+ Yeni Hesap Bağla"* dendiğinde Antigravity ilk kurulum modunda (temiz profille) açılır.
  * Google hesabınızla giriş yaptığınız anda program yeni jetonu ve JWT kimliğini otomatik algılayıp kaydeder.
* **Mevcut Oturumu Otomatik Tanıma:** İlk çalıştırmada bilgisayarınızda halihazırda açık olan Google hesabını otomatik algılar ve listeye ekler.
* **Tek Tıkla Geçiş (🚀 Bu Hesaba Geç):**
  * İstediğiniz hesaba tıkladığınızda Windows kasası ve profil anında güncellenir, Antigravity yeni hesapla açılır.
* **Hesapları Özelleştirme & Yeniden Adlandırma (✏️):**
  * Hesap adını (örn. *Ana Hesap (Pro)*, *Yedek 1*) ve e-posta etiketini arayüzde dilediğiniz gibi düzenleyebilirsiniz.
* **Masaüstü Kısayolu Üretme (📌):**
  * İstediğiniz hesap için doğrudan o hesabı başlatan özel bir masaüstü kısayolu oluşturabilirsiniz.
* **Proje Entegrasyonu:**
  * Hedef proje klasörünüzü belirleyebilirsiniz; geçiş yapıldığında Antigravity doğrudan o projeyi açar.
* **Kod ve Proje Güvenliği:**
  * Kodlarınız ve dosyalarınız yerel diskinizde saklandığı için hesap değişimlerinde **asla silinmez veya zarar görmez**.

---

## 🛠️ Teknolojiler

* **Platform:** Windows (x64)
* **Dil & Çatı:** C# • .NET 10.0 WPF (Windows Presentation Foundation)
* **Tasarım:** Modern Dark Theme (Catppuccin / VS Code Palette)
* **Windows API:** `Advapi32.dll` P/Invoke (`CredReadW`, `CredWriteW`, `CredDeleteW`)
* **Veri Yönetimi:** `System.Text.Json`

---

## 🚀 Kurulum ve Derleme

### Gereksinimler
* Windows 10 / 11 (64-bit)
* [.NET 10.0 SDK](https://dotnet.microsoft.com/)

### Derleme Adımları
```powershell
# Projeyi klonlayın
git clone https://github.com/emrecanardcc/AntigravityManager.git
cd AntigravityManager

# Geliştirme modunda derleyin
dotnet build

# Tek dosya bağımsız .exe üretin
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

Derlenen `publish\AntigravityManager.exe` dosyasını doğrudan çalıştırabilirsiniz.

---

## 📄 Lisans
Bu proje [MIT](LICENSE) lisansı ile lisanslanmıştır.
