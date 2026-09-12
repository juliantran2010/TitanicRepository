VAR has_binoculars = false

The icy Atlantic wind howls across the open deck. A sailor stands by the foremast ladder, shivering violently in his heavy watch coat.
Sailor Hogg: Halt there! Passengers are not permitted near the foremast ladders! It is freezing out here!

+ {has_binoculars} [Show the binoculars and offer to climb]
    You: Look here — I recovered the naval binoculars from Blair's cupboard!
    Sailor Hogg: <color=\#D8A47F>(eyes widen)</color> The lookout glasses?! Fleet and Lee have been freezing half to death up in that nest for two hours without sight!
    You: Let me take them up. I can scan the horizon while they warm their hands.
    Sailor Hogg: God bless you, you've got guts! Fleet's hands are numb — he can barely hold a mug, let alone focus lenses. Climb up into the nest and find out what's out there!
    # add_quest:spot_iceberg:Climb to the Crow's Nest and scan for icebergs
    # teleport:TitanicScene:titanic_lookout
    -> END

+ {!has_binoculars} [Ask about the mast ladder]
    You: Does that ladder lead up to the crow's nest?
    Sailor Hogg: Aye, but you have no business up there. The watchmen are squinting into the pitch-black night with their bare eyes. Head back inside into the warm!
    -> END

+ [Step back inside]
    You: The wind is bitter. I will step back inside.
    Sailor Hogg: Aye, hurry before the frost bites your fingers off.
    -> END
