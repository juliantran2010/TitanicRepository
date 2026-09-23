VAR has_binoculars = false

{ has_binoculars:
    Lookout_Fleet: What in God's name are you doing up here?!
    Lookout_Fleet: You shouldn't have climbed all the way up the mast in this freezing wind! What happened down there?

    You: I found the key to the second officer's locker. I have the missing binoculars!

    Lookout_Fleet: The binoculars?! The ones everyone said were locked away since Southampton?!
    Lookout_Fleet: Sweet Lord, we've been straining our bare eyes into pitch blackness for hours! Hand them over, quick!

    # sfx:rustle_binoculars
    # anim:use_binoculars
    Lookout_Fleet: Let's see what is lurking out there in this haze...
    Lookout_Fleet: Calm water... not a breath of wind to break against the ice...
    Lookout_Fleet: Nothing on the horizon, just—

    # sfx:heartbeat_swell
    Lookout_Fleet: Wait.
    Lookout_Fleet: ...
    
    # anim:sheer_panic
    Lookout_Fleet: Sweet Jesus Christ... Right ahead!
    Lookout_Fleet: A berg! A massive wall of ice right in our path!

    # sfx:bell_triple_ring
    # cam:look_bridge
    Lookout_Fleet: (shouting down to the bridge) ICEBERG, RIGHT AHEAD!!
    Lookout_Fleet: (screaming at the top of his lungs) HARD A-STARBOARD, FOR GOD'S SAKE!! HARD A-STARBOARD!!

    # sfx:foghorn_blast
    # trigger:ship_turn_evasion
    # complete_quest:save_titanic
    # ui:victory_screen
    System: Collision averted.
    System: Thanks to the binoculars and your warning, the helm answers just in time.
    System: The Titanic glides safely past the towering wall of ice. You have rewritten history.

- else:
    Lookout_Fleet: What in God's name are you doing up here?!
    Lookout_Fleet: Clear out! Passengers have no business up on the mast!
    Lookout_Fleet: It's pitch black, freezing cold, and we can barely see past the bow without any glasses.
    Lookout_Fleet: Get back down to safety before you slip and fall!
}