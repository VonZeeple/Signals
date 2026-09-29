List of new blocks
==================

Signal Emitters
---------------

Source
^^^^^^

Emits a signal of magnitude 15.

Signal routing
--------------

Switch
^^^^^^

When on, conducts signal without attenuation. When off, blocks signal. On/off state is changed when activated by a player, a ticker block or an actuator.


Button
^^^^^^

Similar to a switch, but returns to off state once interaction is finished.

Delay
^^^^^

The delay outputs a signal of the same level than the signal applied at its input, with a delay. The delay is configured by changing the cursor position.

Pressure Plate
^^^^^^^^^^^^^^

Transmits signal with no attenuation when colliding with an item or entity.

Valve
^^^^^

This block has three connections: an anode, a cathode and a grid. signal is transmitted only from cathode to anode with an attenuation given by the signal value on the grid.

Connectors
^^^^^^^^^^

Used to connect wires.

Resistors
^^^^^^^^^

Decreases the signal level by its value.


Signal receivers
----------------

Actuator
^^^^^^^^

When powered with a signal of strength greater than 0, it activates the block in front of it.

Buzzer
^^^^^^

When powered, emits a continous sound. The pitch can be adjusted using right-click.

Screen
^^^^^^

Signal meter
^^^^^^^^^^^^

Light Bulb
^^^^^^^^^^

Other blocks
------------

Bell
^^^^

Rings when activated.

WIP blocks
----------

Anemometer (wind detector)
^^^^^^^^^^^^^^^^^^^^^^^^^^
Outputs a signal proportional to the wind speed.

Breadboard
^^^^^^^^^^
Allows creation of miniature circuits (not implemented yet).