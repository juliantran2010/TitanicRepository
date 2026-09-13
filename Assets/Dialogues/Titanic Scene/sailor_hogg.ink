VAR has_binoculars = false
VAR rung_bell = false

{ rung_bell:
    -> after_bell
- else:
    -> before_bell
}

=== before_bell ===
# complete_quest:explore_deck
The icy Atlantic wind howls across the open deck. A sailor stands by the foremast ladder, shivering violently in his heavy watch coat.
Sailor_Hogg: Halt there! Passengers are not permitted near the foremast ladders! It is freezing out here!

+ {has_binoculars} [Show the binoculars and offer to climb]
    You: Look here — I recovered the naval binoculars from Blair's cupboard!
    Sailor_Hogg: <color=\#D8A47F>(eyes widen)</color> The lookout glasses?! Fleet and Lee have been freezing half to death up in that nest for two hours without sight!
    You: Let me take them up. I can scan the horizon while they warm their hands.
    Sailor_Hogg: God bless you, you've got guts! Fleet's hands are numb — he can barely hold a mug, let alone focus lenses. Climb up into the nest and find out what's out there!
    # add_quest:spot_iceberg:Climb to the Crow's Nest and scan for icebergs
    # teleport:TitanicScene:titanic_lookout
    -> END

+ [Ask about the mast ladder]
    You: Does that ladder lead up to the crow's nest?
    Sailor_Hogg: Aye, but you have no business up there. The watchmen are squinting into the pitch-black night with their bare eyes. Head back inside into the warm!
    -> END

+ [Step back inside]
    You: The wind is bitter. I will step back inside.
    Sailor_Hogg: Aye, hurry before the frost bites your fingers off.
    -> END

=== after_bell ===
#complete_quest:talk_to_hogg
A violent tremor shakes the deck plates under your feet as the engines strain into reverse.
Sailor_Hogg: <color=\#D8A47F>(rushes toward you, eyes wide)</color> Was that you ringing the bell?! Did you spot ice?!

+ [Tell him about the iceberg]
    You: Yes! A massive iceberg dead ahead! The bridge heard the bell and Murdoch is already turning the helm!
    Sailor_Hogg: God Almighty... you can feel the hull groaning! The rudder is hard over, but she turns slow!
    Sailor_Hogg: If we brush that ice, we'll need every ship in this sector steaming to us!
    Sailor_Hogg: Run directly to the Marconi Room behind the bridge! Jack Phillips must send an emergency ice dispatch before other ships shut their wireless down for the night!
    # add_quest:warn_marconi:Run to the Marconi Room and send an emergency ice alert
    -> END

+ [Ask if the ship can clear it]
    You: Can the ship make the turn in time?!
    Sailor_Hogg: Murdoch is trying, but don't just stand here staring! We need rescue standing by if the plates buckle!
    Sailor_Hogg: Run to the Marconi Room behind the bridge! Force Jack_Phillips to broadcast an emergency warning right now!
    # add_quest:warn_marconi:Run to the Marconi Room and send an emergency ice alert
    -> END