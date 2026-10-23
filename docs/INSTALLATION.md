# Nem installation · Android Live 0.0.101

Del **Android-Live-0.0.101-Setup.exe** med hver bruger. Det er én samlet installationsfil. Der skal ikke pakkes ZIP-filer ud, vælges mapper eller installeres udviklingsværktøjer.

1. Gem præsentationen, og luk PowerPoint.
2. Dobbeltklik på **Android-Live-0.0.101-Setup.exe**.
3. Klik **Installer / opdater**, og følg eventuelle Microsoft-vinduer.
4. Når installationsprogrammet skriver **Færdig**, start PowerPoint normalt.

Installationsfilen skal køres af den Windows-bruger, som skal bruge PowerPoint; brug ikke Kør som administrator. Den placerer selv programmet i en kort, fast mappe under brugerens lokale appdata. Den samme fil kan deles med flere brugere på hver deres pc. Den er ikke en central MSI/Intune-installation til alle brugere på en pc.

## Forudsætninger og første forbindelse

Windows 10 (1607 eller nyere)/11, 64-bit, med desktop-PowerPoint. Installationsvinduet kræver .NET Framework 4.6.2 eller nyere (normalt allerede på disse Windows-versioner). Hvis selve Office-tilføjelsen mangler .NET Framework 4.8 eller VSTO Runtime, starter Microsofts komponentinstallation. Den kan kræve internet, administratorhjælp og genstart. Efter genstart køres den samme installationsfil igen.

scrcpy følger endnu ikke med installationsfilen. Android Live finder en eksisterende installation; ellers viser fanen den officielle installationsvejledning. Brugeren skal også aktivere Trådløs fejlfinding og godkende tabletparringen første gang. OBS Studio er ikke nødvendigt.

## Opdatering og fjernelse

Kør den nye versions installationsfil på samme måde. Programmet kontrollerer PowerPoint, validerer hele pakken og flytter kendte tidligere Office-installationer til den faste placering. Tabletindstillinger og PPTX-filer bevares.

Ved en afbrudt Office-installation forsøger programmet at gendanne gamle programfiler og registreringen. Statusvinduet fortæller, hvis geninstallationen af den gamle udgave ikke lykkes. Backups beholdes til fejlfinding. Hvis den gamle kildeplacering mangler, eller der er modstridende registreringer, kan Windows' Installerede apps stadig være nødvendige; programmet fjerner ikke ukendte installationer.

Fjern **HJVTAK Android Live / HjvTak.LiveAndroid** via Windows' Installerede apps for at deaktivere tilføjelsen. De udpakkede programfiler/backups og tabletindstillinger bevares lokalt.

## Før bred deling

Denne udgave er en udviklingsudgave. Den ydre EXE er endnu ikke offentligt udgiversigneret, og Office-manifester bruger et udviklingscertifikat. Windows/Microsoft kan vise en advarsel om ukendt udgiver. Brugerne skal selv vurdere tillidsprompter; installationsprogrammet ændrer ikke sikkerhedsindstillinger og tilføjer ikke certifikater til betroede udgivere.

Før bred distribution skal en ansvarlig udgiver levere et offentligt signeringscertifikat eller en tilsvarende godkendt signeringstjeneste, og vi skal teste installation/opdatering på rene pc'er. Central IT-udrulning og lydløs installation er ikke implementeret.

Den gamle ZIP-pakke er kun til fejlfinding/udvikling. Brug den samlede EXE til almindelige brugere.
## På undervisningsdagen

Åbn din gemte PowerPoint på den samme pc/brugerprofil. Tænd Trådløs fejlfinding på tablets, og forbind dem til samme Wi-Fi som pc'en. Tryk **Klargør / forbind alle**.

Programmet forsøger først den gemte adresse og derefter annoncerede Wi-Fi-enheder, som matcher den kendte enheds identitet. Første gang efter opgradering skal en gammel enhed muligvis opdateres med QR, så identiteten kan læses.

Hvis en enhed ikke bliver fundet: vælg den i **Tablet**, og tryk **Opdater valgt / QR**. Scan en ny QR-kode, eller skriv forbindelsesadressen fra tabletten. Brug denne knap til eksisterende enheder; **Tilføj tablet / QR** er til nye enheder. Navn og slide-tilknytninger bevares.

En ny enhed får sit Android-navn, fx HJVTAKT001; hvis Android ikke oplyser det, bruges modellen eller Tablet 1 osv. **Omdøb enhed** giver et eget navn, som huskes.

QR-koden bruges kun til parring og vises i en kort session. Den gemmes ikke i præsentationen og forlænges ikke til 24 timer. Android styrer parringsgodkendelsen og kan slå trådløs fejlfinding fra eller ændre forbindelsesadressen. Ny QR er kun nødvendig, hvis den tidligere parring/genforbindelse ikke længere virker. Netværk, der blokerer mDNS eller trafik mellem klienter, kan kræve manuel adresse eller et andet Wi-Fi.

Enhedslisten tilhører pc'ens brugerprofil. Flytning af PPTX til en anden pc overfører ikke enhedslisten. Livebilleder er fortsat eksterne vinduer; de bliver ikke til indlejret video eller en del af PDF/videoeksport.