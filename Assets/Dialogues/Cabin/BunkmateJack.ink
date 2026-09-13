VAR convinced_lightoller = false
VAR convinced_iceberg = false

The sailor lies in the narrow bunk, staring wearily at the low ceiling. A glint of brass is visible beneath his rough pillow.
Sailor_Jack: <color=\#D8A47F>(frowns)</color> A passenger down in the crew berths? What in bloody blazes are you doing here?

-> dialogue_hub

=== caught_stealing ===
As you reach your hand toward the pillow, Jack's arm shoots out, slapping your wrist away!
Sailor_Jack: <color=\#D9534F>(snarls)</color> Keep your hands to yourself! That belongs in my custody, you bloody thief! What do you think you're doing? Explain yourself!
-> dialogue_hub

=== dialogue_hub ===
+ {!convinced_lightoller} [Claim Officer Lightoller sent you]
    You: Officer Lightoller personally sent me down here. Blair left the cupboard key in your custody, and the bridge needs it!
    Sailor_Jack: <color=\#D8A47F>(frowns)</color> Lightoller? An officer sending a passenger instead of a crew boy? Sounds fishy to me. Why would the bridge need that key right this second?
    ~ convinced_lightoller = true
    -> check_progress

+ {!convinced_iceberg} [Warn him about the speed and ice field]
    You: We are steaming at 22 knots directly into reported ice fields without binoculars! If we hit an iceberg, this berth is right at the waterline!
    Sailor_Jack: <color=\#D8A47F>(shifts uneasily)</color> Ice fields at 22 knots? That's mad! But Blair told me to guard that key under my bolster until the bridge officially asks for it.
    ~ convinced_iceberg = true
    -> check_progress

+ [Bluntly demand: "Give me the key under your pillow!"]
    You: Stop talking and just hand over the brass key under your pillow!
    Sailor_Jack: <color=\#D9534F>(glares)</color> Not a chance! I don't take orders from passengers. Give me a proper reason or get lost.
    -> dialogue_hub

+ [Try to grab the key by force]
    -> caught_stealing

+ [Step back and leave for now]
    You: I will come back in a moment.
    Sailor_Jack: Aye, mind the bulkhead.
    -> END

=== check_progress ===
{
    - convinced_lightoller and convinced_iceberg:
        -> give_key_permission
    - else:
        -> dialogue_hub
}

=== give_key_permission ===
Sailor_Jack: <color=\#D8A47F>(eyes widen, sitting upright)</color> Wait... Lightoller sent you because the lookouts are blind in an ice field?!
Sailor_Jack: Sweet mother of God... that explains why the watch is straining up there! Blair shoved that heavy brass key into my hand before he left, told me to guard it for the bridge.
Sailor_Jack: Go on, take it! It's right there under my bolster. Run it to the Mailroom cupboard before we end up at the bottom of the Atlantic!
# set:can_interact_cupboard_key:true
# complete_quest:find_key
# add_quest:pickup_cupboard_key:Take the cupboard key from under Jack's pillow
-> END