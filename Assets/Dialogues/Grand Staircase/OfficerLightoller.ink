VAR has_clue_board = false
VAR has_clue_rumor = false

Officer Lightoller: Evening. May I assist you? Passengers are advised to remain indoors where it is heated.

+ [Ask general questions]
    You: Good evening, Officer. How is the voyage going?
    Officer Lightoller: Smooth and swift. We are making excellent time toward New York.
    -> END

+ {has_clue_board} [Confront about speed and ice]
    You: Officer, the notice board reports ice fields ahead, yet our engines are at full speed.
    Officer Lightoller: Captain Smith has everything under control. Though... our watch is indeed forced to rely on bare eyes tonight.
    -> explain_blair

+ {has_clue_rumor} [Confront about missing binoculars]
    You: Officer, passengers say the lookouts have no binoculars. Can that be true?
    Officer Lightoller: <color=\#D8A47F>(frowns)</color> Word travels fast. Yes, unfortunately it is the grim truth.
    -> explain_blair

+ [Excuse yourself and leave]
    You: Excuse me, Officer. I won't take up any more of your time.
    Officer Lightoller: Good evening to you. Watch your footing on the deck.
    -> END

=== explain_blair ===
You: Why would a flagship like the Titanic sail without glasses for the watch?
Officer Lightoller: Second Officer Blair was reassigned to the Olympic at the last minute. In his haste, he took the equipment locker key with him.
Officer Lightoller: The glasses are locked tight in Mail Locker 4.
You: Did Blair leave any trace of the key behind?
Officer Lightoller: Blair shared a cabin bunk with Seaman Jack down in the Crew Berths on C-Deck before he left. If anyone knows where that key ended up, it's Jack.
# complete_quest:investigate_ship
# add_quest:find_key:Find the key (Ask Blair's bunkmate Jack in the Crew Berths)
-> END