You: Excuse me, could you tell me where I can find an officer?
Gentleman: Of course, I'll be happy to help.
Gentleman: What would you like to know exactly?

-> choices

=== choices ===

+ [Ask about an officers' lounge]
    You: Is there a lounge nearby where officers often spend their time?
    Gentleman: A lounge? Certainly not.
    Gentleman: Officers are strictly on duty, not relaxing with guests. That won't help you find one.
    -> choices

+ [Ask about first-class dinner times]
    You: Do you know what time dinner is served in the first-class dining room?
    Gentleman: Dinner has been over for hours.
    Gentleman: Besides, an officer wouldn't be having supper right now anyway. That leads nowhere.
    -> choices

+ [Ask for directions to the bridge]
    You: Could you tell me where the bridge is? I need to speak with an officer.
    Gentleman: Take that door over there – it will lead you straight outside onto the boat deck.
    Gentleman: Once you step outside, just follow the deck all the way forward toward the bow.
    Gentleman: Keep walking until you reach the very end of the deck. You can't miss the bridge up there.
    # complete_quest:staircase_officer
    # add_quest:bridge_find_officer:Go to the bridge to find an officer
    -> END

+ [Ask for the captain's quarters]
    You: Could you direct me to the captain's quarters?
    Gentleman: Good heavens, you can't just wander into the captain's private cabin!
    Gentleman: It's completely locked, so don't even bother looking there.
    -> choices

+ [Ask about the lifeboats]
    You: Do you know where I can find the lifeboats?
    Gentleman: Why on earth would you look for an officer at the lifeboats on a calm night like this?
    Gentleman: No one is stationed out there right now.
    -> choices

+ [Ask for directions to the smoking room]
    You: Which way is the smoking room?
    Gentleman: That's entirely the wrong direction if you're looking for ship personnel.
    Gentleman: Only passengers are in the smoking room at this hour.
    -> choices