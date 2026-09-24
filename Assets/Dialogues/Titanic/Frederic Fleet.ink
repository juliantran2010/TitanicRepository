VAR has_binoculars = false

{ has_binoculars:
    Frederick_Fleet: What in God's name are you doing up here?!
    Frederick_Fleet: You shouldn't have climbed all the way up the mast in this freezing wind! What happened down there?

    You: I found the key to the second officer's locker. I have the missing binoculars!

    Frederick_Fleet: The binoculars?! The ones everyone said were locked away since Southampton?!
    Frederick_Fleet: Sweet Lord, we've been straining our bare eyes into pitch blackness for hours! Hand them over, quick!

    + [Hand over the binoculars]
        # trigger:put_down_binoculars:pause
        You: Here, take them!
        Frederick_Fleet: Let's see what is lurking out there in this haze...
        Frederick_Fleet: Calm water... not a breath of wind to break against the ice...
        Frederick_Fleet: Nothing on the horizon, just—

        Frederick_Fleet: Wait.
        Frederick_Fleet: ...
        
        # cam:titanic:0
        Frederick_Fleet: Sweet Jesus Christ... Right ahead!
        Frederick_Fleet: A berg! A massive wall of ice right in our path!

        # cam:titanic:1
        Frederick_Fleet: (shouting down to the bridge) ICEBERG, RIGHT AHEAD!!
        # cam:titanic:2
        Frederick_Fleet: (screaming at the top of his lungs) HARD A-STARBOARD, FOR GOD'S SAKE!! HARD A-STARBOARD!!

        # sfx:foghorn_blast
        # trigger:ship_turn_evasion
        # trigger:victory_screen
        System: Collision averted.
        System: Thanks to the binoculars and your warning, the helm answers just in time.
        System: The Titanic glides safely past the towering wall of ice. You have rewritten history.
        -> END

- else:
    Frederick_Fleet: What in God's name are you doing up here?!
    Frederick_Fleet: Clear out! Passengers have no business up on the mast!
    Frederick_Fleet: It's pitch black, freezing cold, and we can barely see past the bow without any glasses.
    Frederick_Fleet: Get back down to safety before you slip and fall!
    -> END
}