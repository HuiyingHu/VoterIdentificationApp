using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.IO;
using System.Globalization;

namespace VoterIdentificationApp
{
    /// <summary>
    /// MainWindow.xaml 
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// replace your subscription key here
        /// </summary>
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("58bcb00181d84c059c248d788f66fa2e");

        /// <summary>
        /// Tab 1 Browse test image in section 'Detect Faces'
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        private async Task<FaceRectangle[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    return faceRects.ToArray();
                }
            }
            catch (Exception)
            {
                return new FaceRectangle[0];
            }
        }
        /// <summary>
        /// Tab 1 Detect Faces and draw rectangles for detected faces in section 'Detect Faces'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DetectFacesBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
            {
                return;
            }

            string filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            FacePhoto.Source = bitmapSource;

            Title = "Detecting...";
            FaceRectangle[] faceRects = await UploadAndDetectFaces(filePath);
            Title = String.Format("Detection Finished. {0} face(s) detected", faceRects.Length);

            if (faceRects.Length > 0)
            {
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                double resizeFactor = 96 / dpi;

                foreach (var faceRect in faceRects)
                {
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            faceRect.Left * resizeFactor,
                            faceRect.Top * resizeFactor,
                            faceRect.Width * resizeFactor,
                            faceRect.Height * resizeFactor
                            )
                    );
                }

                drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                FacePhoto.Source = faceWithRectBitmap;
            }

        }

        /// <summary>
        /// Tab2 Create a person group in section 'Define Person Group'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DefinePersonGroupButton_Click(object sender, RoutedEventArgs e)
        {
            bool flag1 = string.IsNullOrWhiteSpace(Tab2PersonGroupIdTextBox.Text);
            bool flag2 = string.IsNullOrWhiteSpace(Tab2PersonGroupNameTextBox.Text);
            if (flag1)
            {
                MessageBox.Show("Please input the person group id");
                Tab2PersonGroupIdTextBox.Focus();
                
            }
            if (flag2)
            {
                MessageBox.Show("Please input the person group name");
                Tab2PersonGroupNameTextBox.Focus();
            }
            if (!flag1 && !flag2)
            {
                string personGroupId = Tab2PersonGroupIdTextBox.Text;
                string personGroupName = Tab2PersonGroupNameTextBox.Text;
                await faceServiceClient.CreatePersonGroupAsync(personGroupId, personGroupName);
                MessageBox.Show("Succeed define person group");
            }     
        }
        /// <summary>
        /// This function works for all comboxes in binding person group datasource in all tabControl pages.
        /// Dynamic combine defined person group ID with combox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void tabControl_SelectionChanged(object sender, RoutedEventArgs e)
        {
            PersonGroup[] personGroupDefined = await faceServiceClient.ListPersonGroupsAsync("0");
            if (personGroupDefined.Length < 1)
            {
                MessageBox.Show("Please input the person group id");
            }
            if (personGroupDefined.Length >= 1)
            {
                if (tabControl.SelectedIndex == 2)
                {
                    foreach (PersonGroup p in personGroupDefined)
                    {
                        string groupId = p.PersonGroupId;
                        Tab3PersonGroupIDComboBox.Items.Add(groupId);
                    }
                    
                }

                if (tabControl.SelectedIndex == 3)
                {
                    
                    foreach (PersonGroup p in personGroupDefined)
                    {
                        string groupId = p.PersonGroupId;
                        Tab4PersonGroupIDComboBox.Items.Add(groupId);
                    }
                }

                if (tabControl.SelectedIndex == 4)
                {
                    
                    foreach (PersonGroup p in personGroupDefined)
                    {
                        string groupId = p.PersonGroupId;
                        Tab5PersonGroupIDComboBox.Items.Add(groupId);
                    }
                } 
            }
            
        }
        /// <summary>
        /// Tab 3 Browse a dirctory contains images of register person in section 'Register Person'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegisterPersonBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.Tab3DirectoryOfRegisterPersonTextBox.Text = fbd.SelectedPath;
            }

        }
        /// <summary>
        /// Tab 3 Register a person with a bunch of registered face images in section 'Register Person'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async void ResigterPersonButton_Click(object sender, RoutedEventArgs e)
        {
            int itemNum = Tab3PersonGroupIDComboBox.Items.Count;
            int selectedGroup = Tab3PersonGroupIDComboBox.SelectedIndex;
            bool flag1 = string.IsNullOrWhiteSpace(Tab3PersonNameTextBox.Text);
            bool flag2 = string.IsNullOrWhiteSpace(Tab3DirectoryOfRegisterPersonTextBox.Text);
            if (itemNum == 0)
            {
                MessageBox.Show("Please define the person group ID first in page 2");
            }
            if (flag1)
            {
                MessageBox.Show("Please input the person name");
                Tab3PersonNameTextBox.Focus();
            }

            if (flag2)
            {
                MessageBox.Show("Please select a directory of register voter's images");
            }
            if (selectedGroup == -1)
            {
                MessageBox.Show("Please select a group");
            }
            if (itemNum!=0 && !flag1 &&!flag2 && selectedGroup != -1)
            {
                string personGroupId = Tab3PersonGroupIDComboBox.Text;
                string personName = Tab3PersonNameTextBox.Text;
                CreatePersonResult myPerson = await faceServiceClient.CreatePersonAsync(personGroupId, personName);
                foreach (string imagePath in Directory.GetFiles(Tab3DirectoryOfRegisterPersonTextBox.Text))
                {
                    using (Stream s = File.OpenRead(imagePath))
                    {
                        await faceServiceClient.AddPersonFaceAsync(personGroupId, myPerson.PersonId, s);
                    }
                }

                MessageBox.Show("Succeed register person");
            }

        }

        /// <summary>
        /// Tab 4 Train the person group in section 'Train'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async void TraniPersonGroupButton_Click(object sender, RoutedEventArgs e)
        {
            int itemNum = Tab4PersonGroupIDComboBox.Items.Count;
            if (itemNum == 0)
            {
                MessageBox.Show("Please define the person group ID first in page 2");
            }
            if (itemNum != 0)
            {
                string personGroupId = Tab4PersonGroupIDComboBox.Text;
                await faceServiceClient.TrainPersonGroupAsync(personGroupId);

                TrainingStatus trainingStatus = null;
                while (true)
                {
                    trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                    if (trainingStatus.Status != Status.Running)
                    {
                        if (trainingStatus.Status == Status.Failed)
                        {
                            MessageBox.Show("Train failed");
                            break;
                        }
                        if (trainingStatus.Status == Status.Succeeded)
                        {
                            MessageBox.Show("Train succeeded");
                            break;
                        }
                        await Task.Delay(1000);                      
                    }
                }
                MessageBox.Show("Succeed train person group");
            }    
        }
        /// <summary>
        /// Tab 5 Identify voters with face rectangles and voters'name in section 'Identify Voters'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            int itemNum = Tab5PersonGroupIDComboBox.Items.Count;
            int selectedGroup = Tab5PersonGroupIDComboBox.SelectedIndex;
            if (itemNum == 0)
            {
                MessageBox.Show("Please define the person group ID first in page 2");
            }
            if (selectedGroup == -1)
            {
                MessageBox.Show("Please select a group.");
            }
            if (itemNum != 0 && selectedGroup != -1)
            {
                TestFacePhoto.Source = null;
                var openDlg = new Microsoft.Win32.OpenFileDialog();

                openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
                bool? result = openDlg.ShowDialog(this);

                if (!(bool)result)
                {
                    return;
                }

                string filePath = openDlg.FileName;

                Uri fileUri = new Uri(filePath);
                BitmapImage bitmapSource = new BitmapImage();

                bitmapSource.BeginInit();
                bitmapSource.CacheOption = BitmapCacheOption.None;
                bitmapSource.UriSource = fileUri;
                bitmapSource.EndInit();


                using (Stream s = File.OpenRead(filePath))
                {
                    var faces = await faceServiceClient.DetectAsync(s);
                    var faceRects = faces.Select(face => face.FaceRectangle).ToArray();
                    var faceIds = faces.Select(face => face.FaceId).ToArray();

                    string personGroupId = Tab5PersonGroupIDComboBox.Text;

                    var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);

                    int i = 0;
                    FaceRectangle faceRect = null;

                    DrawingVisual visual = new DrawingVisual();
                    DrawingContext drawingContext = visual.RenderOpen();
                    drawingContext.DrawImage(bitmapSource,
                        new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                    double dpi = bitmapSource.DpiX;
                    double resizeFactor = 96 / dpi;

                    foreach (var identifyResult in results)
                    {
                        
                        Console.WriteLine("Identify result of face id: {0}", identifyResult.FaceId);
                        Console.WriteLine("Identify result of length of candidates: {0}", identifyResult.Candidates.Length);

                        faceRect = faceRects[i];

                        
                        ///No candidate indicates that the person has not provided his registered face image to register as a voter.
                        if (identifyResult.Candidates.Length == 0)
                        {
                            ///Draw a face rectangle
                            drawingContext.DrawRectangle(
                                            Brushes.Transparent,
                                            new Pen(Brushes.Red, 2),
                                            new Rect(
                                                faceRect.Left * resizeFactor,
                                                faceRect.Top * resizeFactor,
                                                faceRect.Width * resizeFactor,
                                                faceRect.Height * resizeFactor
                                                )
                                        );
                            ///Draw a context with 'Unknow' in 'Ghostwhite' color
                            Point point = new Point(faceRect.Left * resizeFactor, faceRect.Top * resizeFactor -10);
                            string text = "Unknow";
                            System.Windows.Media.FormattedText myText = new System.Windows.Media.FormattedText(
                                text,
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Marlet"),
                                8.0,
                                System.Windows.Media.Brushes.GhostWhite);
                            drawingContext.DrawText(myText, point);


                        }

                        ///The Candidate.Length != 0 means that this person with a bunch of registered face images has been succeed registered into this system
                        if (identifyResult.Candidates.Length != 0)
                        {
                            var candidateId = identifyResult.Candidates[0].PersonId;
                            var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                            Console.WriteLine("Identified as {0}", person.Name);

                            ///Draw a face rectangle
                            drawingContext.DrawRectangle(
                                            Brushes.Transparent,
                                            new Pen(Brushes.Red, 2),
                                            new Rect(
                                                faceRect.Left * resizeFactor,
                                                faceRect.Top * resizeFactor,
                                                faceRect.Width * resizeFactor,
                                                faceRect.Height * resizeFactor
                                                )
                                        );

                            ///Draw a context with register person's name in 'GreenYellow' colour
                            Point point = new Point(faceRect.Left * resizeFactor, faceRect.Top * resizeFactor -10);
                            string text = person.Name;
                            System.Windows.Media.FormattedText myText = new System.Windows.Media.FormattedText(
                                text,
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                new Typeface("Marlet"),
                                10.0,
                                System.Windows.Media.Brushes.GreenYellow);
                            drawingContext.DrawText(myText, point);
                        }
                        i++;
                    }
                    drawingContext.Close();
                    RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                        (int)(bitmapSource.PixelWidth * resizeFactor),
                        (int)(bitmapSource.PixelHeight * resizeFactor),
                        96,
                        96,
                        PixelFormats.Pbgra32);

                    faceWithRectBitmap.Render(visual);
                    TestFacePhoto.Source = faceWithRectBitmap;
                } 
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        
    }
}
