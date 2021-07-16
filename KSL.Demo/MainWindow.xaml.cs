namespace KSL.Demo
{
    using KSL.Gestures.Classifier;
    using KSL.Gestures.Core;
    using KSL.Gestures.Logger;
    using KSL.Gestures.Segments;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.WpfViewers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;
    using System.Timers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
     using System.Windows.Media;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region "Declaration"

        private KSLConfig config = new KSLConfig();
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private Skeleton[] skeletons = new Skeleton[0];
        private GesturesController gestureController;

        public event PropertyChangedEventHandler PropertyChanged;
        
        Classifier classifier = Classifier.getInstance;
        private Logger logger = Logger.getInstance;

        private string gestureSentence;
        private string gestureBuilder;
        Timer startStopTimer;
        private bool isNewSentence = false;

        #endregion

        #region "Constructor"

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            startStopTimer = new Timer(2000);
            startStopTimer.Elapsed += new ElapsedEventHandler(startStopTimer_Elapsed);
            InitializeKinect();
         
           
            
        }

        #endregion

        #region "Initialize Kinect"

        private void InitializeKinect()
        {
            // initialize the Kinect sensor manager
            KinectSensorManager = new KinectSensorManager();
            KinectSensorManager.KinectSensorChanged += this.KinectSensorChanged;

            // locate an available sensor
            sensorChooser.Start();

            // bind chooser's sensor value to the local sensor manager
            var kinectSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.KinectSensorManager, KinectSensorManager.KinectSensorProperty, kinectSensorBinding);
        }

        private void KinectSensorChanged(object sender, KinectSensorManagerEventArgs<KinectSensor> args)
        {
            if (null != args.OldValue)
                UninitializeKinectServices(args.OldValue);

            if (null != args.NewValue)
                InitializeKinectServices(KinectSensorManager, args.NewValue);
        }

        private void InitializeKinectServices(KinectSensorManager kinectSensorManager, KinectSensor sensor)
        {
            kinectSensorManager.ColorFormat = config.colorImageFormat;
            kinectSensorManager.ColorStreamEnabled = true;

            kinectSensorManager.DepthStreamEnabled = true;

            kinectSensorManager.TransformSmoothParameters = config.transformSmoothParameters;

            sensor.SkeletonFrameReady += OnSkeletonFrameReady;
            kinectSensorManager.SkeletonStreamEnabled = true;

            kinectSensorManager.KinectSensorEnabled = true;

            if (!kinectSensorManager.KinectSensorAppConflict)
            {
                gestureController = new GesturesController();
                gestureController.GestureRecognized += OnGestureRecognized;

                RegisterGestures();
            }
        }

        private void UninitializeKinectServices(KinectSensor sensor)
        {
            sensor.SkeletonFrameReady -= OnSkeletonFrameReady;
            gestureController.GestureRecognized -= OnGestureRecognized;
        }

        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                // resize the skeletons array if needed
                if (skeletons.Length != frame.SkeletonArrayLength)
                    skeletons = new Skeleton[frame.SkeletonArrayLength];

                // get the skeleton data
                frame.CopySkeletonDataTo(skeletons);

                foreach (var skeleton in skeletons)
                {
                    // skip the skeleton if it is not being tracked
                    if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                        continue;

                    // update the gesture controller
                    gestureController.UpdateAllGestures(skeleton);
                }
            }
        }

        public static readonly DependencyProperty KinectSensorManagerProperty = DependencyProperty.Register
        (
            "KinectSensorManager",
            typeof(KinectSensorManager),
            typeof(MainWindow),
            new PropertyMetadata(null)
        );

        public KinectSensorManager KinectSensorManager
        {
            get { return GetValue(KinectSensorManagerProperty) as KinectSensorManager; }
            set { SetValue(KinectSensorManagerProperty, value); }
        }

        #endregion

        #region "Gestures - Register, Recognize"

        private void RegisterGestures()
        {
            // Word: Merhaba
            IGesturesSegment[] helloSegments = new IGesturesSegment[2];
            HelloSegment1 helloSegment1 = new HelloSegment1();
            HelloSegment2 helloSegment2 = new HelloSegment2();
            helloSegments[0] = helloSegment1;
            helloSegments[1] = helloSegment2;
            gestureController.AddGesture("Merhaba", helloSegments);

        

            // Word: Yiyecek
            IGesturesSegment[] foodSegments = new IGesturesSegment[4];
            FoodSegment1 foodSegment1 = new FoodSegment1();
            foodSegments[0] = foodSegment1;
            foodSegments[1] = foodSegment1;
            foodSegments[2] = foodSegment1;
            foodSegments[3] = foodSegment1;
            gestureController.AddGesture("Yiyecek", foodSegments);

            // Word: yaş
            IGesturesSegment[] ageSegments = new IGesturesSegment[2];
            AgeSegment1 ageSegment1 = new AgeSegment1();
            AgeSegment2 ageSegment2 = new AgeSegment2();
            ageSegments[0] = ageSegment1;
            ageSegments[1] = ageSegment2;
            gestureController.AddGesture("Yaş", ageSegments);

        }

        private void OnGestureRecognized(object sender, GesturesEventArgs e)
        {
            switch (e.GestureName)
            {
                case "Merhaba":
                    display(WordsEnum.Merhaba);
                    break;
                case "Yaş":
                    display(WordsEnum.Yaş);
                    break;
                case "Yiyecek":
                    display(WordsEnum.Yiyecek);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region "Display text"

        private void reset()
        {
            classifier.reset();
        }

        private void display(WordsEnum word)
        {
            classifier.addCode((int) word);
            string sentence = classifier.findSentence();
            List<int> sentenceBuilder = classifier.getSentenceBuilder();

            if (!isNewSentence)
            {
                if (sentenceBuilder.Count > 1 && !String.IsNullOrEmpty(sentence))
                {
                    isNewSentence = true;
                    startStopTimer.Start();
                    StringBuilder sb = new StringBuilder();
                    GestureBuilder = String.Empty;
                    foreach (int wordCode in sentenceBuilder)
                    {
                        sb.Append(Enum.GetName(typeof(WordsEnum), wordCode));
                        sb.Append(" ● ");
                    }
                    sb.Length = sb.Length - 3;
                    GestureBuilder = sb.ToString();
                    GestureSentence = sentence;
                }
                else
                {
                    GestureSentence = String.Empty;
                    GestureBuilder = Enum.GetName(typeof(WordsEnum), word);
                }
            }
        }

        public String GestureSentence
        {
            get { return gestureSentence; }

            private set
            {
                if (gestureSentence == value)
                    return;

                gestureSentence = value;

                if (this.PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("GestureSentence"));
            }
        }

        public String GestureBuilder
        {
            get { return gestureBuilder; }

            private set
            {
                if (gestureBuilder == value)
                    return;

                gestureBuilder = value;

                if(this.PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("GestureBuilder"));
            }
        }

        private void startStopTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            startStopTimer.Stop();
            this.isNewSentence = false;
        }

        #endregion


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            myWeb.Visibility = Visibility.Visible;
           myWeb.Source = new Uri ("http://m.YollaYap.com/bbtc/1558645709903.gif") ;

            
            

        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            myWeb.Visibility = Visibility.Visible;
            myWeb.Source = new Uri("https://media.tenor.com/images/ed3cf00e1c90206e943f68d349877690/tenor.gif");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            myWeb.Visibility = Visibility.Visible;
            myWeb.Source = new Uri("http://4.bp.blogspot.com/_2ehL4qctoPs/TLY7_gaLDII/AAAAAAAAHro/1aGyWG7gkUA/s1600/erkek-kopek-kisirlastirma.gif");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            myWeb.Visibility = Visibility.Visible;
            myWeb.Source = new Uri("https://i.pinimg.com/originals/45/f4/8f/45f48f33d0918f347ebe874931215f4a.gif");
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            myWeb.Visibility = Visibility.Visible;
            myWeb.Source = new Uri("https://pa1.narvii.com/6591/1a7335837c677583959994fce15139055233b144_hq.gif");

        }
        
    }
}
