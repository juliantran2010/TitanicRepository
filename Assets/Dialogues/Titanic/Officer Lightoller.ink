VAR quest_active_show_lightoller_mesaba = false
VAR quest_completed_warn_lightoller = false
VAR quest_active_order_game = false
VAR quest_completed_order_game = false

{ quest_active_order_game:
    Second_Officer_Lightoller: Go down to the Mail Room and find the binoculars! That is your first priority.
        Second_Officer_Lightoller: Check the chart table over there, pick your tasks, and hurry!
    -> END
}

{ quest_completed_order_game:
    Second_Officer_Lightoller: Exactly. Get the binoculars first, then take them to the lookouts. Hurry!
    # add_quest:goto_MailScene_mail:Go to the Mail Room to get the binoculars.>pickup_binoculars:Try to find hints where the binoculars could be.
    # guide:mail
    -> END
}

{ quest_active_show_lightoller_mesaba:
    + [Hand Over Mesaba Message.]
        # complete_quest:show_lightoller_mesaba
        Second_Officer_Lightoller: I believe you. This is a serious warning.
        Second_Officer_Lightoller: (shouts to Crew) Quartermaster, stand by! Helmsman, change course!
        Second_Officer_Lightoller: (looking at you) You! I need your help right now.
        Second_Officer_Lightoller: Take care of the lookout's eyes. You will find them in ... (loud disturbing sound)
        Second_Officer_Lightoller: Check the chart table over there, pick your tasks, put them in the right order and hurry!
        # add_quest:order_game:Go to the chart table and pick your tasks in the correct order.
        -> END
    + [Leave.]
        -> END
- else:
    { not quest_completed_warn_lightoller:
        # complete_quest:bridge_find_officer
        You: Excuse me, could you tell me where I can find an officer? I need to warn you. The Titanic is in danger.
        Second_Officer_Lightoller: Stop there! Passengers are not allowed on the bridge. What are you doing here?
        
        # add_quest:warn_lightoller:Convince Second Officer Lightoller and give a clear warning.
        # trigger:sentence_minigame:pause
        # complete_quest:warn_lightoller
    }
    // Läuft IMMER hierhin (egal ob frisch aus dem Minigame oder beim erneuten Ansprechen):
    Second_Officer_Lightoller: We already received reports of ice. Why should I believe you?
    Second_Officer_Lightoller: You need to give me a clear and convincing reason.
}
