VAR quiz_score = 0

The Marconi transmitter crackles with electric sparks.
Jack_Phillips: Out of the way! I have a stack of passenger telegrams for Cape Race!

+ [Interrupt him with the alert]
    You: Stop the passenger traffic, Phillips! We just spotted an enormous iceberg right in our course! The helm is fighting to steer clear right now!
    Jack_Phillips: <color=\#D8A47F>(freezes, wide-eyed)</color> Iceberg?! Directly in our path?!
    You: Yes! We need to send an emergency navigational hazard alert to all nearby ships immediately so they stay alert and stand by!
    Jack_Phillips: Murdoch is already executing an emergency turn! Quick, what distress prefix do I open the transmission with?
    -> choose_prefix

=== choose_prefix ===
+ [Recommend only 'CQD']
    You: Send 'CQD'! It's the White Star and Marconi company standard!
    Jack_Phillips: Right, the British ships will copy CQD, but German ships might hesitate... Tapping CQD!
    ~ quiz_score = quiz_score + 1
    -> choose_callsign

+ [Recommend only 'SOS']
    You: Transmit 'SOS'! It's the newer international standard!
    Jack_Phillips: Modern regulations, good! Though some older British operators still scan for CQD first... Tapping SOS!
    ~ quiz_score = quiz_score + 1
    -> choose_callsign

+ [Recommend combining both: 'CQD and SOS']
    You: Send both! 'CQD' for the British lines and 'SOS' for all foreign vessels!
    Jack_Phillips: <color=\#D8A47F>(nods sharply)</color> Brilliant thinking! That way no operator in the North Atlantic ignores us.
    ~ quiz_score = quiz_score + 2
    -> choose_callsign

+ [Tell him to send a standard commercial CQ]
    You: Just send a normal 'CQ' message asking for clear channels.
    Jack_Phillips: <color=\#D9534F>Are you mad?!</color> A routine CQ has zero priority, operators will tell us to shut up!
    ~ quiz_score = quiz_score + 0
    -> choose_callsign

=== choose_callsign ===
Jack_Phillips: Now verify our station callsign for the header! What is Titanic's Marconi code?

+ [State 'MGY']
    You: Our callsign is 'MGY'! Marconi Great Britain!
    Jack_Phillips: Exactly! 'MGY' confirmed on the key!
    ~ quiz_score = quiz_score + 2
    -> choose_coordinates

+ [State 'WSL']
    You: Isn't it 'WSL' for White Star Line?
    Jack_Phillips: No, that's not our Marconi identifier! But there's no time to argue, I'll log it as MGY anyway!
    ~ quiz_score = quiz_score + 0
    -> choose_coordinates

+ [State 'TIT']
    You: Just tap 'TIT' for Titanic!
    Jack_Phillips: That's completely invalid naval shorthand! Stations will think it's a garbled transmission!
    ~ quiz_score = quiz_score + 0
    -> choose_coordinates

=== choose_coordinates ===
Jack_Phillips: Final piece! Give me our exact grid position so ships can plot our course!

+ [State Baltic Notice coordinates: 41°46'N, 50°14'W]
    You: 41°46' North, 50°14' West! Advise them to reduce speed and stand by!
    Jack_Phillips: Coordinates logged from the Baltic report. Transmitting emergency dispatch now!
    ~ quiz_score = quiz_score + 2
    -> evaluate_results

+ [Give estimated coordinates: 40°20'N, 49°10'W]
    You: 40°20' North, 49°10' West! Close enough to our track!
    Jack_Phillips: That puts us almost 80 miles off position! If someone steams out to assist, they'll be searching empty ocean! Transmitting anyway...
    ~ quiz_score = quiz_score + 0
    -> evaluate_results

+ [Vague estimate: "Near the Grand Banks!"]
    You: Just broadcast that we are approaching the Grand Banks!
    Jack_Phillips: That's far too vague for a naval hazard! Navigators can't calculate bearing with that! Transmitting general alert...
    ~ quiz_score = quiz_score + 0
    -> evaluate_results

=== evaluate_results ===
# trigger:play_morse_audio
<color=\#9E9E9E>*Dit-dit-dit, dah-dah-dah, dit-dit-dit...*</color>
The transmitter sparks fiercely into the night.

{
    - quiz_score >= 5:
        -> outcome_perfect
    - quiz_score >= 2:
        -> outcome_mediocre
    - else:
        -> outcome_failure
}

=== outcome_perfect ===
Jack_Phillips: Immediate acknowledgments coming in! The SS Californian and Carpathia are reading us loud and clear!
Jack_Phillips: The Californian is keeping her wireless room manned and both ships are dropping speed to stand by!
Through the wheelhouse window, the colossal wall of ice glides safely past the starboard beam — missing the hull by over a hundred yards.
# complete_quest:warn_marconi
# trigger:game_victory
<color=\#F4D06F><b>[TIMELINE PRESERVED - PERFECT SIGNAL]</b></color>
Flawless radio procedure and early visual warning ensured full naval cooperation. The Atlantic is on alert, and the Titanic sails safely into New York.
-> END

=== outcome_mediocre ===
Jack_Phillips: <color=\#D8A47F>(frowns, listening to the headphones)</color> The Frankfurt is asking for clarification... they're confused by the format of our callsign and coordinates!
Jack_Phillips: Wait — the Carpathia just cut through the noise and confirmed our position, but we lost critical minutes in the exchange!
A deafening screech of displaced water echoes outside. Through the window, chunks of razor-sharp ice scrape harmlessly along the bilge keel, missing the bulkheads by mere inches.
# complete_quest:warn_marconi
# trigger:game_victory
<color=\#F4D06F><b>[TIMELINE PRESERVED - CLOSE CALL]</b></color>
The warning got through just in time despite procedural confusion. The Titanic cleared the obstacle with minimal margin, preserving the timeline.
-> END

=== outcome_failure ===
Jack_Phillips: <color=\#D9534F><b>(shouts in panic)</b></color> Chaos on the frequency! Cape Race is telling us to stop transmitting, and the Californian didn't copy our faulty grid!
Jack_Phillips: Nobody understands where we are or that an emergency is underway!
A shuddering impact reverberates through the keel plates as ice scrapes along the lower compartments...
# trigger:game_over
<color=\#D9534F><b>[TIMELINE FRACTURED]</b></color>
Due to garbled signals and inaccurate navigational data, the surrounding fleet remained oblivious. The warning was lost in the noise.
-> END