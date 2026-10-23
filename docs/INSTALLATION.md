# Installation · Android Live 0.0.100

1. Pak hele **Android-Live-0.0.100-installation.zip** ud til en fast mappe, fx Dokumenter/Android Live. Behold alle filer og mapper sammen.
2. Gem dit arbejde, og luk PowerPoint.
3. Åbn den udpakkede mappe i Stifinder, og dobbeltklik på **setup.exe**. Kør ikke direkte fra ZIP-filen.
4. Følg Microsofts installation. Udviklingsudgaven er endnu ikke signeret med et offentligt udgivercertifikat; vurder selv Microsofts tillidsprompt.
5. Start PowerPoint normalt. Fanen **Android Live** viser version **0.0.100**.

Du behøver ikke Visual Studio eller projektets kildekode for at installere. Windows x64, desktop-PowerPoint, .NET Framework 4.8 og Microsoft VSTO Runtime kræves. Setup kontrollerer .NET Framework 4.8 og VSTO Runtime; internet og eventuelt administratorhjælp kan kræves til forudsætninger. scrcpy installeres separat via vejledningen i Android Live, hvis det ikke findes.

## Opdatering fra udviklingsinstallationen 0.0.006

Hvis Microsoft skriver, at løsningen allerede er installeret fra en anden placering, fjern **HJVTAK Android Live** under Windows' Installerede apps, og kør den nye setup.exe. Fjern kun denne tilføjelse. Tabletindstillingerne i din brugerprofil og rammerne i dine PowerPoint-filer bevares. Slet ikke din indstillingsmappe.

Fremtidige pakker udpakkes til samme faste installationsmappe. Luk PowerPoint før opdatering, og kør setup.exe igen. Automatisk download/opdatering er ikke slået til. En ny udviklingssignatur kan give en ny tillidsprompt.

## På undervisningsdagen

Åbn din gemte PowerPoint på den samme pc/brugerprofil. Tænd Trådløs fejlfinding på tablets, og forbind dem til samme Wi-Fi som pc'en. Tryk **Klargør / forbind alle**.

Programmet forsøger først den gemte adresse og derefter annoncerede Wi-Fi-enheder, som matcher den kendte enheds identitet. Første gang efter opgradering skal en gammel enhed muligvis opdateres med QR, så identiteten kan læses.

Hvis en enhed ikke bliver fundet: vælg den i **Tablet**, og tryk **Opdater valgt / QR**. Scan en ny QR-kode, eller skriv forbindelsesadressen fra tabletten. Brug denne knap til eksisterende enheder; **Tilføj tablet / QR** er til nye enheder. Navn og slide-tilknytninger bevares.

En ny enhed får sit Android-navn, fx HJVTAKT001; hvis Android ikke oplyser det, bruges modellen eller Tablet 1 osv. **Omdøb enhed** giver et eget navn, som huskes.

QR-koden bruges kun til parring og vises i en kort session. Den gemmes ikke i præsentationen og forlænges ikke til 24 timer. Android styrer parringsgodkendelsen og kan slå trådløs fejlfinding fra eller ændre forbindelsesadressen. Ny QR er kun nødvendig, hvis den tidligere parring/genforbindelse ikke længere virker. Netværk, der blokerer mDNS eller trafik mellem klienter, kan kræve manuel adresse eller et andet Wi-Fi.

Enhedslisten tilhører pc'ens brugerprofil. Flytning af PPTX til en anden pc overfører ikke enhedslisten. Livebilleder er fortsat eksterne vinduer; de bliver ikke til indlejret video eller en del af PDF/videoeksport.