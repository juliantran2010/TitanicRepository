// Globale Variable in Ink definieren
VAR hasKey = false

-> kapitaen_dialog

=== kapitaen_dialog ===
Kapitän Smith: Ahoi! Was gibt es?
You: Hallo, alles gut

{hasKey:
    Kapitän Smith: Oh, du hast den Schlüssel gefunden! Hervorragend.
- else:
    Kapitän Smith: Wir brauchen immer noch den Schlüssel für die Kabine.
}
-> END