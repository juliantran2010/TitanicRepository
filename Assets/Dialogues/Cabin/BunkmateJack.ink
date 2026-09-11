The sailor lies in the narrow bunk, staring wearily at the low ceiling. A glint of brass is visible beneath his rough pillow.
Sailor Jack: <color=\#D8A47F>(frowns)</color> A passenger? Down in the crew berths at this hour? What in bloody blazes are you sneaking around here for?

+ [State your business directly]
    You: I am looking for Second Officer David Blair's belongings.
    Sailor Jack: Blair packed his kit days ago and caught the tender to the Olympic. Nothing of his left here for you, mate.
    -> berth_options

+ [Ask who he is]
    You: Good evening. Are you alright?
    Sailor Jack: Just came off an eight-hour deck watch. My bones are frozen solid. You shouldn't be down here.
    -> berth_options

+ [Excuse yourself and leave]
    You: My apologies, I took a wrong turn. Rest well.
    Sailor Jack: Aye, mind the hatch on your way out.
    -> END

// ==========================================
// EINSTIEGSPUNKT BEIM UNERLAUBTEN GREIFEN:
// C# ruft: story.ChoosePathString("caught_stealing")
// ==========================================
=== caught_stealing ===
As you reach your hand toward the pillow, Jack's arm shoots out, slapping your wrist away! # complete_quest:find_key
Sailor Jack: <color=\#D8A47F>(snarls)</color> Keep your hands to yourself! That belongs to me, you bloody thief! What do you think you're doing?
-> berth_options

=== berth_options ===
+ [Demand the key on Lightoller's authority]
    You: Officer Lightoller sent me. Second Officer Blair left the crow's nest locker key with you, and the watch needs it immediately!
    Sailor Jack: Lightoller sent you himself? Blimey, then it really is serious...
    -> give_key_permission

+ [Warn him about the pack ice and full speed]
    You: We are steaming full speed into reported ice fields without binoculars! If we strike a berg, this compartment will flood before anyone else even notices!
    Sailor Jack: Pack ice at 22 knots?! God's truth, that's suicide!
    -> give_key_permission

+ [Ask innocently about the key]
    You: What is that brass key under your pillow anyway?
    Sailor Jack: None of your business, mate! I don't hand over things entrusted to me to just any stranger.
    -> berth_options

+ [Apologize and step back]
    You: I apologize. I will leave you to rest.
    Sailor Jack: Aye, back to the passenger cabins with you. Keep your hands off my gear.
    -> END

=== give_key_permission ===
Sailor Jack: <color=\#D8A47F>(sighs)</color> Blast it all... you're right. Blair shoved that heavy brass key into my hands right before he left. Told me to guard it under my bolster until someone from the bridge asks for it.
Sailor Jack: Go on then, take it! It's right there under the pillow. Get it to the Mailroom before the Captain runs us into a berg!
# set:can_interact_cupboard_key:true
# complete_quest:find_key
# add_quest:pickup_cupboard_key:Take the locker key from under Jack's pillow
-> END