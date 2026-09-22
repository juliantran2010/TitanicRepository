VAR mesaba_solved = false

{ mesaba_solved:
    # complete_quest:show_lightoller_mesaba
    Second_Officer_Lightoller: I believe you. This is a serious warning.
    Second_Officer_Lightoller: (shouts to Crew) Quartermaster, stand by! Helmsman, prepare to alter course!
    
    Second_Officer_Lightoller: (looking at you) And you. I need every pair of hands. Take care of the lookout's eyes!
    Second_Officer_Lightoller: The bridge crew is occupied with steering.
    Second_Officer_Lightoller: Here are the current station orders. Pick out the tasks meant for YOU, put them in order, and get moving!
    # add_quest:chart_table_minigame:Go to the chart table and pick your tasks in the correct order

- else:
    // Fall 2: Spieler hat die Beweise noch nicht
    # complete_quest:bridge_find_officer
    You: Excuse me, could you tell me where I can find an officer? I need to warn you. The Titanic is in danger.
    Second_Officer_Lightoller: Stop there! Passengers are not allowed on the bridge. What are you doing here?
    
    # add_quest:warn_lightoller:Convince Second Officer Lightoller and give a clear warning.
    # trigger:sentence_minigame:pause
    # complete_quest:warn_lightoller
    
    Second_Officer_Lightoller: We already received reports of ice. Why should I believe you?
    Second_Officer_Lightoller: You need to give me a clear and convincing reason.
}