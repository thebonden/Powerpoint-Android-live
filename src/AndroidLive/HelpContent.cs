using System;
using System.Collections.Generic;
using System.Linq;
using AndroidLive.Core;

namespace AndroidLive
{
    internal sealed class HelpTopic
    {
        internal string Id {get;}
        internal string Title {get;}
        internal string Body {get;}
        internal HelpTopic(string id,string title,string body){Id=id;Title=title;Body=body;}
        public override string ToString()=>Title;
    }
    internal static class HelpContent
    {
        internal static readonly HelpTopic[] Topics = {
            new HelpTopic("start","Kom godt i gang",
                "1. Åbn Android Live-fanen. Brug en Android-tablet med trådløs fejlfinding, normalt Android 11 eller nyere.\n\n"+
                "2. Tilslut pc og tablet til samme Wi-Fi. Åbn Udviklerindstillinger → Trådløs fejlfinding på tabletten. Menuernes navne kan variere.\n\n"+
                "3. Klik Tilføj tablet / QR i PowerPoint. Vælg parring med QR-kode under trådløs fejlfinding på tabletten, og scan koden dér.\n\n"+
                "4. Vælg tabletten på listen. Skriv eventuelt Dit navn, og tryk Enter.\n\n"+
                "5. Klik Indsæt / opdater ramme på den ønskede slide. Flyt og tilpas rammen, og start slideshowet.\n\n"+
                "Gemte tablets forsøges genforbundet ved næste opstart. Forbind valgt genstarter en gemt forbindelse uden QR."),
            new HelpTopic("picture","Forbundet, men intet billede",
                "1. Kontrollér den valgte tablet i listen. Prøv Forbind valgt og derefter Forhåndsvisning.\n\n"+
                "2. Virker forhåndsvisningen, men ikke slideshowet? Slå Vis livebilleder til. Vælg samme tablet som rammen tilhører, og brug Indsæt / opdater ramme på sliden.\n\n"+
                "3. Brug én almindelig rektangulær ramme pr. tablet på samme slide. Rammen skal være helt inden for sliden, synlig og uden rotation. Undgå animationer på rammen.\n\n"+
                "4. Klik tilbage i slideshowet. Sort/hvid skærm, pause, minimering og et andet program i forgrunden skjuler livevinduerne. Kør kun ét slideshow ad gangen.\n\n"+
                "5. Virker forhåndsvisningen heller ikke? Åbn Forbindelse og genforbindelse her til venstre og se Status og detaljer.\n\n"+
                "Tablettens besked om en aktiv trådløs forbindelse bekræfter ikke i sig selv, at videoen vises i den rigtige PowerPoint-ramme."),
            new HelpTopic("waiting","Tomt felt eller ventetekst",
                "Det tomme, mørke felt er tilsigtet de første 15 sekunder uden livebillede. Både logo og tekst holdes skjult i den periode.\n\n"+
                "Efter 15 sekunder vises tabletikonet og Venter på forbindelse til med enhedsnavn og dit eget navn. Videoen erstatter feltet, så snart den er klar; den venter ikke på de 15 sekunder.\n\n"+
                "Hver tablet har sin egen ventetid. Et nyt forbindelsesudfald eller et nyt besøg på sliden starter ventetiden forfra.\n\n"+
                "Ser du den gamle redigeringstekst kortvarigt? Kontrollér versionen i dette vindue. 0.0.112 rettede ventefeltets opstart; rettelsen er også med i denne version. Særlige overgange og animerede rammer skal afprøves på din opsætning."),
            new HelpTopic("connection","Forbindelse og genforbindelse",
                "1. Kontrollér Wi-Fi på begge enheder, og at trådløs fejlfinding stadig er slået til på tabletten. Hold tabletten vågen under testen.\n\n"+
                "2. Vælg den gemte tablet og klik Forbind valgt. Ved flere tablets: Klargør / forbind alle. Det bevarer navn og tilknytningen til dine slides.\n\n"+
                "3. Hvis der tidligere er trykket Stop valgt eller Stop alle, er automatisk genforbindelse slået fra for disse enheder. Forbind valgt aktiverer den igen.\n\n"+
                "4. Hvis netværket blev skiftet, eller enheden ikke findes: kontrollér, at enhederne må tale sammen. Gæste- og virksomhedsnet kan blokere dette; bed netværksansvarlig om hjælp.\n\n"+
                "5. Prøv først Opdater valgt / QR, hvis den almindelige genforbindelse ikke hjælper, eller parringen er fjernet. Vælg den eksisterende tablet, så gamle slides bevarer deres tilknytning.\n\n"+
                "Lukning af PowerPoint alene slår ikke automatisk genforbindelse fra. Programmet prøver igen efter forbindelsesfejl, men kan ikke forhindre netværksudfald eller dvale."),
            new HelpTopic("qr","QR-koden virker ikke",
                "1. Scan i Trådløs fejlfinding → Par enhed med QR-kode på tabletten. Brug ikke den almindelige kameraapp.\n\n"+
                "2. Kontrollér, at både pc og tablet bruger samme Wi-Fi. Hold QR-vinduet åbent under parringen.\n\n"+
                "3. Luk QR-vinduet og åbn en ny kode, hvis koden er udløbet. En udløbet QR-kode betyder ikke nødvendigvis, at en tidligere parring er væk; prøv Forbind valgt først.\n\n"+
                "4. Ved en kendt tablet: brug Opdater valgt / QR. Tilføj tablet / QR er til en ekstra enhed.\n\n"+
                "5. Hvis tabletten ikke tilbyder trådløs fejlfinding, kan Android-versionen eller enhedens administration begrænse funktionen. Kontakt den ansvarlige for enheden."),
            new HelpTopic("tools","scrcpy mangler eller kan ikke starte",
                "1. Åbn Indstillinger → Find automatisk. Programmet leder efter en komplet scrcpy-installation.\n\n"+
                "2. Hvis scrcpy mangler, følg installationsvejledningen i dialogen. Brug scrcpy 4.1 eller nyere og den komplette Windows-pakke; kopier ikke kun scrcpy.exe.\n\n"+
                "3. Ved manuelt valg: vælg scrcpy.exe fra den udpakkede mappe sammen med dens øvrige filer.\n\n"+
                "4. Prøv Forbind valgt igen. Se Status og detaljer, hvis start stadig fejler, og giv den viste fejl til din IT-support.\n\n"+
                "OBS Studio er ikke nødvendig for Android Live."),
            new HelpTopic("sound","Lyd, fingertryk og orientering",
                "Vælg først den tablet, du vil ændre.\n\n"+
                "Lyd slår tabletlyd til eller fra på pc'en. Kontrollér også tablettens medielyd, Windows-lydstyrke og valgt højttaler. Nogle Android-versioner eller apps kan begrænse lydoptagelse.\n\n"+
                "Vis fingertryk viser fysiske berøringer på tabletten. Slå knappen fra igen for at skjule markeringerne.\n\n"+
                "Orientering kan være Automatisk, Liggende eller Stående. Prøv Automatisk, hvis billedet vender forkert. Hele skærmen tilpasses rammen; sorte kanter er normale ved forskellige billedformater.\n\n"+
                "Ændringer genstarter kort den valgte tablets videoforbindelse. Lyd kan fortsætte, selv om billedet er skjult på en anden slide; slå Lyd fra eller stop tabletten for at standse den."),
            new HelpTopic("devices","Navne, flere tablets og genbrug",
                "Listen viser Android-enhedens navn og dit eget navn. Skriv i Dit navn og tryk Enter, eller brug Ændr navn… for også at kunne følge Android-navnet igen.\n\n"+
                "Tilføj flere tablets, og vælg den rigtige, før du indsætter en ramme. Forskellige tablets kan bruges på samme slide eller på hver sin slide.\n\n"+
                "Gamle rammer opdateres med logo og aktuelle navne ved Indsæt / opdater ramme. Rammen beholder sin placering og enhedstilknytning. Knappen erstatter rammens egen tekst og udseende.\n\n"+
                "Brug Opdater valgt / QR til at genforbinde en gemt tablet. Fjern fra listen fjerner den gemte profil; de gamle rammer kan derefter ikke vise den. Tilføjelse igen giver en ny tilknytning, så de gamle rammer skal erstattes."),
            new HelpTopic("day","Klargøring til en undervisningsdag",
                "1. Installér og afprøv opdateringer før undervisningsdagen. Gem præsentationen og luk PowerPoint før installation.\n\n"+
                "2. På stedet: forbind pc og tablets til undervisningens Wi-Fi. Åbn præsentationen og klik Klargør / forbind alle ved behov.\n\n"+
                "3. Test en slide med hver tablet, skift frem og tilbage, og prøv lyd, orientering og eventuel præsentationsvisning på projektoren.\n\n"+
                "4. Sørg for strøm og en passende skærm-/dvaleindstilling efter stedets regler. Wi-Fi og trådløs fejlfinding skal fortsat være tilgængelige.\n\n"+
                "5. Ved udfald: giv genforbindelsen tid, og følg Forbindelse og genforbindelse, hvis billedet ikke vender tilbage.\n\n"+
                "En hel undervisningsdag skal afprøves på det konkrete netværk. Automatiske tests er ikke en garanti for otte timers fysisk Wi-Fi-forbindelse."),
            new HelpTopic("install","Installation eller manglende fane",
                "1. Gem præsentationen og luk alle PowerPoint-vinduer. Kør Android Live-installationsfilen, og vælg Installer / opdater.\n\n"+
                "2. Åbn PowerPoint igen og se efter Android Live-fanen. Brug installationsprogrammets vejledning, hvis Microsoft-komponenter mangler.\n\n"+
                "3. Hvis fanen mangler: se Filer → Indstillinger → Tilføjelsesprogrammer → Administrer COM-tilføjelsesprogrammer → Udfør. Se også Deaktiverede elementer. Menuernes navne kan variere.\n\n"+
                "4. Hvis organisationen administrerer tilføjelsesprogrammer, kontakt IT med installationsfejlen. Ændr ikke generelle sikkerhedsindstillinger for at få programmet til at indlæse.\n\n"+
                "Android Live er et Windows-tilføjelsesprogram til PowerPoint på skrivebordet. Det er ikke beregnet til PowerPoint i browseren eller på Mac."),
            new HelpTopic("export","Optagelse, eksport og begrænsninger",
                "Livebilledet og ventevisningen ligger i eksterne vinduer oven på slideshowet. De er ikke indlejret video i selve sliden.\n\n"+
                "PowerPoints PDF-/videoeksport og deling af kun slideindholdet medtager derfor ikke livebilledet. Test en eventuel optagelse eller deling af hele skærmen særskilt.\n\n"+
                "Brug statiske, rektangulære rammer. Animerede rammer og særlige overgange kan vise den underliggende grafik, før livevinduet placeres.\n\n"+
                "OBS Studio kan fortsat bruges som en separat løsning til mere frie forløb; det er ikke en forudsætning for dette tilføjelsesprogram.")
        };
        internal static IEnumerable<HelpTopic> Search(string query)
        {
            string value=(query??"").Trim();
            return Topics.Where(t=>value.Length==0 || (t.Title+" "+t.Body).IndexOf(value,StringComparison.CurrentCultureIgnoreCase)>=0);
        }
        internal static string LocalChecks(bool completeTools,DeviceProfile device,bool overlay,DeviceState state)
        {
            return "Lokale kontroller – netværket er ikke testet her\n\n"+
                (completeTools?"✓ scrcpy-pakken er fundet.":"• scrcpy-pakken mangler. Åbn Indstillinger → Find automatisk.")+"\n\n"+
                (device==null?"• Ingen tablet er valgt. Brug Tilføj tablet / QR.":"Valgt tablet: "+device.ListLabel+
                "\n"+(device.ReconnectOnStartup?"Automatisk genforbindelse er slået til.":"Automatisk genforbindelse er stoppet. Brug Forbind valgt.")+
                "\nVideostatus: "+(state==DeviceState.Running?"Kører":state==DeviceState.Starting?"Starter":state==DeviceState.Faulted?"Fejl – se detaljer":"Stoppet"))+
                "\n\n"+(overlay?"✓ Vis livebilleder er slået til.":"• Vis livebilleder er slået fra. Slå knappen til ved brug af slideshow.")+
                "\n\nDette kontrollerer ikke Wi-Fi, enhedens strøm, den aktuelle ramme eller lyden. Følg den relevante vejledning, hvis billedet stadig mangler.";
        }
    }
}