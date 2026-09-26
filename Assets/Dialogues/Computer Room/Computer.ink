// --- VARIABLEN FÜR DIE EINSTELLUNGEN ---
VAR chosen_location = ""
VAR chosen_time = ""

-> step_1_location

// ==========================================
// SCHRITT 1: ORT WÄHLEN (WHERE)
// ==========================================
=== step_1_location ===
Companion: Where do we need to send you? Look at the navigation map.

+ [New York City, USA]
    Companion: New York is the destination harbor, but the ship never made it there!
    Companion: There will be no Titanic for you to warn in New York. Pick the location of the voyage itself.
    -> step_1_location
    
+ [Lake Superior, North America]
    Companion: Lake Superior is a freshwater lake inside North America! 
    Companion: The Titanic was a transatlantic ocean liner. Check the map again!
    -> step_1_location
    
+ [North Atlantic Ocean (between Europe and North America)]
    ~ chosen_location = "North Atlantic"
    Companion: Exactly! That is where the iceberg field is located.
    -> step_2_time

+ [Southampton, England]
    Companion: Southampton? If we send you there, you'll just be at the docks on departure day.
    Companion: You wouldn't be able to prevent the collision out on the open sea from there!
    Companion: Look closely at where the ship actually hit the ice. Try again.
    -> step_1_location

// ==========================================
// SCHRITT 2: ZEIT WÄHLEN (WHEN)
// ==========================================
=== step_2_time ===
Companion: Good, location locked to the North Atlantic. Now: WHEN should you arrive?

+ [10 April 1912 - 12:00 p.m.]
    Companion: That's days before the iceberg warnings even arrive!
    Companion: The time machine battery won't hold your temporal anchor that long. We need to jump right into the critical phase of the journey!
    Companion: Choose a moment closer to the danger zone.
    -> step_2_time

+ [14 April 1912 - 9:30 p.m.]
    ~ chosen_time = "14 April 1912, 9:30 p.m."
    Companion: Perfect timing! That gives you about two hours before the collision to save the people on the Titanic!
    -> step_3_confirmation

+ [14 April 1912 - 11:45 p.m.]
    Companion: Too late! The lookout strikes the bell at 11:40 p.m., and the ship hits the ice moments later!
    Companion: If you arrive at 11:45 p.m., the hull is already ripped open. We need you to be there EARLIER.
    -> step_2_time

+ [15 April 1912 - 2:20 a.m.]
    Companion: Are you insane?! At 2:20 a.m. the stern plunges into the freezing ocean!
    Companion: You'd materialize directly into freezing water! Pick a time before the tragedy strikes!
    -> step_2_time


// ==========================================
// SCHRITT 3: BESTÄTIGUNG & TELEPORT
// ==========================================
=== step_3_confirmation ===
Time_Machine: All parameters entered:
Time_Machine: <color=\#5CF59B>Destination:</color> RMS Titanic
Time_Machine: <color=\#5CF59B>Location:</color> {chosen_location}
Time_Machine: <color=\#5CF59B>Arrival Time:</color> {chosen_time}
Time_Machine: Are you ready to activate the time machine?

+ [ACTIVATE TIME MACHINE]
    Time_Machine: Initiating quantum sequence... Hold on!
    You: Here goes nothing...
    # teleport:CabinScene:cabin
    -> END

+ [Wait, let me change the settings.]
    Companion: Alright, resetting the sequence. Be thorough.
    -> step_1_location