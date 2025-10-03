# Offichat Server

Bu proje, başlangıç olarak **Offichat** adlı oyunum için geliştirmekte olduğum; fakat TCP ve UDP üzerinden çok oyunculu oyun veya chat uygulamaları için de kullanılabilecek temel bir **asenkron ve session tabanlı sunucu altyapısıdır**.

---

## Özellikler

- **TCP ve UDP desteği**  
  - TCP üzerinden login ve önemli paketler.  
  - UDP üzerinden hızlı paketler (ör. hareket, pozisyon güncelleme).  

- **Session Yönetimi**  
  - PlayerSession ile kullanıcı ve bağlantı bilgilerini yönetme.  
  - AFK ve timeout kontrolleri otomatik.  

- **Handler Sistemi**  
  - IPacketHandler tabanlı handler’lar reflection ile otomatik register ediliyor.  
  - Yeni paket türleri ve handler’lar kolayca eklenebilir.  

---

## Geliştirilecek Özellikler / İyileştirmeler

1. **Logging ve Exception Yönetimi**  
   - TCP ve UDP paketleri için daha kapsamlı logging.  
   - Hataların detaylı kaydını tutmak ve gerekirse yeniden deneme mekanizması eklemek.

2. **Packet Türleri ve Versioning**  
   - Yeni paket türleri eklerken uyumluluğu korumak.  
   - Paket sürümlerini yönetmek için bir versiyon numarası veya schema tanımlamak.

3. **Unit Testler**  
   - SessionManager, PacketRouter ve handler'lar için testler yazmak.  
   - Mock TCP/UDP client kullanarak paket iletimini test etmek.

4. **Handler Yönetimi**  
   - Handler'ların otomatik yüklenmesi için reflection veya dependency injection kullanmak.  
   - Yeni handler eklemeyi kolaylaştırmak.

5. **Performans Optimizasyonu**  
   - UDP ve TCP paketlerinin asenkron işlenmesini optimize etmek.  
   - Gereksiz kopyalamaları ve bloklamaları önlemek.  
   - Session cleanup için foreach yerine daha optimize yöntemler kullanmak.

6. **Güvenlik Önlemleri**  
   - Paket doğrulama ve client doğrulama mekanizması eklemek.  
   - DoS saldırılarını önlemek için rate limiting uygulamak.

7. **Monitoring / Metrics**  
   - Aktif session sayısı, paket gönderim/alım istatistikleri gibi metrikleri toplamak.  
   - Basit bir web dashboard üzerinden izlemek.

8. **Heartbeat / Keepalive (opsiyonel)**  
   - Client’ın gerçekten bağlantıda olup olmadığını kontrol etmek için heartbeat paketleri eklemek.

---
