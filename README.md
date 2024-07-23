# Railroader Mod: FlyShuntUI

Make [fly shunting](https://www.youtube.com/watch?v=ihSTqPDITWY) easier by adding buttons to the Road AI panel to disconnect groups of cars bound for the same destination.

![](/ui-screenshot.PNG)

New buttons to disconnect groups of cars:

* **All**: Disconnect _all_ cars with waybills from the **back** of the train
* **-3**: Disconnect the first 3 groups of cars going to 3 unique destinations from the **back** of the train
* **-2**: Disconnect the first 3 groups of cars going to 3 unique destinations from the **back** of the train
* **-1**: Disconnect all cars going to the same destination from the **back** of the train
* **1**: Disconnect all cars going to the same destination from the **front** of the train
* **2**: Disconnect the first 3 groups of cars going to 3 unique destinations from the **front** of the train
* **3**: Disconnect the first 3 groups of cars going to 3 unique destinations from the **front** of the train
* **All**: Disconnect _all_ cars with waybills from the **front** of the train

Important notes:
* The buttons correspond to how many _groups of cars_ not how many _cars_ you want to disconnect. A group of cars is a set of cars coupled together that all have the same waybill destination.
* Only cars with goods waybills for industries and interchanges are considered, not cars in need of repair or selling.

Example usage:

You are using the Road AI helper to move the train forwards at 10mph along the mainline out of Sylvia Interchange. At the front of your train are 3 cars destined for Parson's Tannery SP1, followed by a number of cars for other destinations. From the mainline, you set up the switches so that you can reach SP1. Then you hit the **1** button to disconnect the _first group of cars for the same destination_ from the front of the train, which happens to the three cars going to SP1. After disconnect, the AI will slow the train as it now detects a train in front (the cars bound for SP1). Once momentum carries the three cars to SP1, apply the handbrake on one to stop them, and continue fly shunting the rest of your train.
