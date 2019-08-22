# DancingCoach

The Kinect v2 is used to capture the user and the data is used to give feedback e.g. the recogntion of basic dance steps.
Therefore, the [Kinect SDK](https://www.microsoft.com/en-us/download/details.aspx?id=44561) is needed.
The interfaces can be used to add other dance styles to the Dancing Trainer. These are the IBeatManager, IFeedback and IDancingTrainerComponent. They ensure functions like play, pause and stop to control a song. A sparate class or classes to handle gestures are recommended. To build gestures that might be used to recognize characteristics of a dance style you can follow this [tutorial](https://channel9.msdn.com/Blogs/k4wdev/Custom-Gestures-End-to-End-with-Kinect-and-Visual-Gesture-Builder). It is foreseen that a dancing component that handles one dance style is instantiated from the MainWindow, after a genre and song were selected.
