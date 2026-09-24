VAR has_cupboard_key = false

The sturdy wooden cupboard stands against the bulkhead, secured by a heavy brass padlock. A plaque reads: 'OFFICERS STORES - AUTHORIZED ACCESS ONLY'.

+ {has_cupboard_key} [Unlock the cupboard]
    You insert David Blair's brass key and turn the lock.
    # trigger:open_cupboard
    The lock gives way and the heavy doors swing open! Inside on the shelf, you see a leather case with naval binoculars.
    # complete_quest:unlock_cupboard
    # add_quest:pickup_binoculars:Take the naval binoculars from the cupboard
    -> END

+ [Examine the padlock]
    You rattle the latch, but it won't budge.
    You: <color=\#F4D06F>(thinking)</color> Firmly locked. I need David Blair's cupboard key from the crew berths.
    -> END

+ [Step away and leave it for now]
    You decide to leave the cupboard alone for a moment.
    -> END