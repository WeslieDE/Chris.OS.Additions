# Chris OS Additions
#### Zusatzfunktionen für OpenSimulator

Dieses Module bietet einige zusatzfunktionen für OpenSimulator.
Es fasst mehrere einzelnen funktionen zusammen und wird laufend ergänzt.

Der Hauptzweck dient der Vereinfachung von Scripten. Es gibt aber auch einige weitere Funktionen, die dazu da sind, Serverleistung einzusparen oder Funktionalitäten bereit zu stellen, die es in dieser Form noch nicht gibt.

----

##### Fragen zu einzelnen funktionen

Wenn ihr fragen oder Wünsche zu einzelnen Funktionen habt, kommt mich doch im Discord besuchen. https://discord.com/invite/9jPSWRahgU

----

##### Dokumentationen

Einzelnen Dokumentationen findet ihr hier:

- Scriptfunktionen ( Readme )
- Robust Services ( Readme )
- Region Module ( Readme )

----

##### Installation

Es gibt keinen fertigen Download für dieses Module.
Ihr müsst es euch selber Compilen.

Dies geht mit 3 einfachen Befehlen.

    git clone git://opensimulator.org/git/opensim OpenSimulator
    git clone https://github.com/Sahrea/Chris.OS.Additions.git ./OpenSimulator/addon-modules/Chris.OS.Additions
    cd OpenSimulator

Und dann unter Windows

    runprebuild.bat
    compile.bat

oder unter Linux

    ./runprebuild.sh
    msbuild /p:Configuration=Release

----
Ich wünsche euch viel spaß!
