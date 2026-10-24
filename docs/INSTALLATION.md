# Nem installation · Android Live 0.0.108

Del **Android-Live-0.0.108-Setup.exe** med hver bruger. Det er én samlet installationsfil. Der skal ikke pakkes ZIP-filer ud, vælges mapper eller installeres udviklingsværktøjer.

1. Gem præsentationen, og luk PowerPoint.
2. Dobbeltklik på **Android-Live-0.0.108-Setup.exe**.
3. Klik **Installer / opdater**, og følg eventuelle Microsoft-vinduer.
4. Når installationsprogrammet skriver **Færdig**, start PowerPoint normalt.

Installationsfilen skal køres af den Windows-bruger, som skal bruge PowerPoint; brug ikke Kør som administrator. Den placerer selv programmet i en kort, fast mappe under brugerens lokale appdata. Den samme fil kan deles med flere brugere på hver deres pc. IT kan også udrulle den centralt i brugerkontekst med den separate IT-pakke. Se [IT-vejledningen](IT-DEPLOYMENT.md).

## Forudsætninger og første forbindelse

Windows 10 (1607 eller nyere)/11, 64-bit, med desktop-PowerPoint. Installationsvinduet kræver .NET Framework 4.6.2 eller nyere (normalt allerede på disse Windows-versioner). Hvis selve Office-tilføjelsen mangler .NET Framework 4.8 eller VSTO Runtime, starter Microsofts komponentinstallation. Den kan kræve internet, administratorhjælp og genstart. Efter genstart køres den samme installationsfil igen.

Brug **scrcpy 4.1 eller nyere**. scrcpy følger endnu ikke med installationsfilen. Android Live finder en eksisterende installation; ellers viser fanen den officielle installationsvejledning. Brugeren skal også aktivere Trådløs fejlfinding og godkende tabletparringen første gang. OBS Studio er ikke nødvendigt.

## Opdatering og fjernelse

Kør den nye versions installationsfil på samme måde. Programmet kontrollerer PowerPoint, validerer hele pakken og flytter kendte tidligere Office-installationer til den faste placering. Tabletindstillinger og PPTX-filer bevares.

Ved en afbrudt Office-installation forsøger programmet at gendanne gamle programfiler og registreringen. Statusvinduet fortæller, hvis geninstallationen af den gamle udgave ikke lykkes. Backups beholdes til fejlfinding. Hvis den gamle kildeplacering mangler, eller der er modstridende registreringer, kan Windows' Installerede apps stadig være nødvendige; programmet fjerner ikke ukendte installationer.

Fjern **Android Live / AndroidLive** via Windows' Installerede apps for at deaktivere tilføjelsen. De udpakkede programfiler/backups og tabletindstillinger bevares lokalt.

## Før bred deling

Denne udgave er en udviklingsudgave. Den ydre EXE er endnu ikke offentligt udgiversigneret, og Office-manifester bruger et udviklingscertifikat. Windows/Microsoft kan vise en advarsel om ukendt udgiver. Brugerne skal selv vurdere tillidsprompter; installationsprogrammet ændrer ikke sikkerhedsindstillinger og tilføjer ikke certifikater til betroede udgivere.

Før bred distribution skal en ansvarlig udgiver levere et offentligt signeringscertifikat eller en tilsvarende godkendt signeringstjeneste, og vi skal teste installation/opdatering på rene pc'er. Lydløs IT-installation er implementeret i brugerkontekst. IT skal klargøre forudsætninger og godkende udgiversignaturen; det er ikke en MSI-installation for alle brugere på én gang.

Den gamle ZIP-pakke er kun til fejlfinding/udvikling. Brug den samlede EXE til almindelige brugere.
## På undervisningsdagen

Åbn din gemte PowerPoint på den samme pc/brugerprofil. Tænd Trådløs fejlfinding på tablets, og forbind dem til samme Wi-Fi som pc'en. Tryk **Klargør / forbind alle**.

Programmet forsøger først den gemte adresse og derefter annoncerede Wi-Fi-enheder, som matcher den kendte enheds identitet. Første gang efter opgradering skal en gammel enhed muligvis opdateres med QR, så identiteten kan læses.

Hvis en enhed ikke bliver fundet: vælg den i **Tablet**, og tryk **Opdater valgt / QR**. Scan en ny QR-kode, eller skriv forbindelsesadressen fra tabletten. Brug denne knap til eksisterende enheder; **Tilføj tablet / QR** er til nye enheder. Egne navne og slide-tilknytninger bevares.

En ny enhed får automatisk sit Android-navn, fx Tablet001; hvis Android ikke oplyser det, bruges modellen eller Tablet 1 osv. Gamle standardnavne som **Tablet 1** bliver også opdateret ved næste forbindelse.

Vil du give eller ændre dit eget navn: vælg tabletten i listen, skriv i feltet **Dit navn** ved listen, og tryk **Enter**. Navnet bliver gemt med det samme og huskes ved genforbindelse. Enheden behøver ikke være forbundet. Du kan fx ændre **Hold A** til **Nyt kursus**, når du genbruger slides; rammerne beholder forbindelsen til samme enhed.

Knappen **Ændr navn…** åbner også et navnevindue. Her kan du altid skrive i navnefeltet; når du skriver, vælges dit eget navn automatisk. Klik **Gem**. Vil du igen følge Android-navnet, sæt markeringen i **Brug enhedens Android-navn** og gem. Hvis Android-navnet endnu ikke er læst, hentes det ved næste forbindelse.

Listen viser begge navne, fx **Tablet001 — Undervisning**: først Android-navnet, derefter dit eget navn. Er de ens, vises navnet kun én gang. Hvis Android-navnet endnu ikke er hentet, står det tydeligt sammen med dit eget navn; forbind enheden for at hente navnet.

### Fjern en tablet fra listen

Vælg enheden, klik **Fjern fra listen**, og bekræft. Dens livebillede stoppes, og den fjernes fra den gemte liste og status. Andre enheder fortsætter. Android-parringen fjernes ikke.

Slides og rammer bliver bevaret, men rammer til den fjernede enhed viser ikke længere livebillede. Tilføjer du enheden igen, får den en ny plads i listen; slet dens gamle rammer og indsæt nye med **Indsæt / opdater ramme**. Brug i stedet **Opdater valgt / QR**, hvis du vil genforbinde en eksisterende enhed og beholde rammernes tilknytning.

Navnevalget ændrer hverken navnet på selve Android-enheden eller forbindelsen mellem tablet og slides. Rammen viser fx **Livebillede fra Tablet001 — INSTRUKTØR** og **under slideshow**, uden versionsnummer. Begge navne bruges; ens navne vises kun én gang.

Har du allerede en ramme på sliden: vælg dens tablet i listen, og klik **Indsæt / opdater ramme**. Det opdaterer teksten i den eksisterende ramme uden at indsætte en ekstra ramme eller ændre enhedens slide-tilknytning. Brug samme knap efter navneændringer, når du genbruger en slide. Eventuel tekst, du selv har skrevet inde i tabletrammen, erstattes ved denne handling.

QR-koden bruges kun til parring og vises i en kort session. Den gemmes ikke i præsentationen og forlænges ikke til 24 timer. Android styrer parringsgodkendelsen og kan slå trådløs fejlfinding fra eller ændre forbindelsesadressen. Ny QR er kun nødvendig, hvis den tidligere parring/genforbindelse ikke længere virker. Netværk, der blokerer mDNS eller trafik mellem klienter, kan kræve manuel adresse eller et andet Wi-Fi.

Enhedslisten tilhører pc'ens brugerprofil. Flytning af PPTX til en anden pc overfører ikke enhedslisten. Livebilleder er fortsat eksterne vinduer; de bliver ikke til indlejret video eller en del af PDF/videoeksport.
## Neutralt navn og eksisterende indstillinger

Programmet hedder **Android Live**. Organisationsnavnet er fjernet fra produktnavn, nye programfiler, namespaces, certifikatnavn og nye datamapper. Gamle identifikatorer findes kun i kompatibilitetskoden, så tidligere installerede udgaver, gemte tabletindstillinger og eksisterende slides kan migreres. Dine egne tablet-navne ændres ikke.

Der er to leverancer: **Android-Live-0.0.108-Setup.exe** til almindelig installation og **Android-Live-0.0.108-IT.zip** til IT med samme EXE, lydløse kommandoer, detektion og vejledning.
## Hele skærmen i rammen

Android Live tilpasser automatisk hele Android-skærmen til tabletrammen og forhåndsvisningen. Billedet bevarer sine proportioner og bliver ikke beskåret eller strakt. Hvis rammen og enheden har forskellige formater, vises sorte kanter over/under eller ved siderne. Du kan frit gøre rammen bredere, smallere, større eller mindre. Tilpasningen følger også enhedens rotation.

Dette ændrer kun visningen på pc'en. Indhold, som Android selv skjuler, fx systembjælker i fuldskærmsapps, vises ikke ved denne tilpasning.