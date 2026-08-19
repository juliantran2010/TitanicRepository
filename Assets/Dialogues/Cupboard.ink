VAR hasKey = false

// Gedankengang des Spielers vor dem Schrank
I'm looking at the locked cupboard. 
I probably need a key to open it and reach the binoculars inside.

{ hasKey:
    // Fall 1: Schlüssel ist vorhanden -> Interaktive Auswahl
    + [Use key to open the cupboard]
        You unlock the cupboard with a quiet click and retrieve the binoculars.
        # trigger: open_cupboard
        -> END

    + [Leave it for now]
        You decide to keep the key in your pocket and step back.
        -> END

- else:
    // Fall 2: Kein Schlüssel im Inventar
    Without the key, there's no way to open this right now. I should look around for it.
    -> END
}