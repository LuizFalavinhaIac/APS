using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Diagnostics;

namespace MultiFaceRec
{
    public partial class FormPrincipal : Form
    {
        //Declararation of all variables, vectors and haarcascades
        Image<Bgr, Byte> Frame;
        Capture camera;
        HaarCascade face;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.7d, 0.7d);
        Image<Gray, byte> resultado, faceTreinada = null;
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> treinaImagens = new List<Image<Gray, byte>>();
        List<string> labels= new List<string>();
        List<string> NomePessoas = new List<string>();
        int ContTreina, NumLabels, t;
        string nome, nomes = null;


        public FormPrincipal()
        {
            InitializeComponent();
            //Load haarcascades for face detection
            face = new HaarCascade("haarcascade_frontalface_default.xml");

            try
            {
                //Load of previus trainned faces and labels for each image
                string Labelsinfo = File.ReadAllText(Application.StartupPath + "/Treinamento/TreinamentoLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTreina = NumLabels;
                string LoadFaces;

                for (int tf = 1; tf < NumLabels+1; tf++)
                {
                    LoadFaces = "Face" + tf + ".bmp";
                    treinaImagens.Add(new Image<Gray, byte>(Application.StartupPath + "/Treinamento/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }
            
            }
            catch(Exception ex)
            {
                //MessageBox.Show(e.ToString());
                MessageBox.Show("Nada encontrado no banco de dados!", "Treinamento", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }


        private void button1_Click(object sender, EventArgs e)
        {
            //Initialize the capture device
            camera = new Capture();
            camera.QueryFrame();
            //Initialize the FrameGraber event
            Application.Idle += new EventHandler(FrameGrabber);
            button1.Enabled = false;
        }


        private void button2_Click(object sender, System.EventArgs e)
        {
            try
            {
                //Trained face counter
                ContTreina = ContTreina + 1;

                //Get a gray frame from capture device
                gray = camera.QueryGrayFrame().Resize(500, 360, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                //Face Detector
                MCvAvgComp[][] facesDetectadas = gray.DetectHaarCascade(
                face,
                1.2,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));

                //Action for each element detected
                foreach (MCvAvgComp f in facesDetectadas[0])
                {
                    faceTreinada = Frame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                //resize face detected image for force to compare the same size with the 
                //test image with cubic interpolation type method
                faceTreinada = resultado.Resize(120, 120, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                treinaImagens.Add(faceTreinada);
                labels.Add(textBox1.Text);

                //Show face added in gray scale
                imageBox1.Image = faceTreinada;

                //Write the number of triained faces in a file text for further load
                File.WriteAllText(Application.StartupPath + "/Treinamento/TreinamentoLabels.txt", treinaImagens.ToArray().Length.ToString() + "%");

                //Write the labels of triained faces in a file text for further load
                for (int i = 1; i < treinaImagens.ToArray().Length + 1; i++)
                {
                    treinaImagens.ToArray()[i - 1].Save(Application.StartupPath + "/Treinamento/Face" + i + ".bmp");
                    File.AppendAllText(Application.StartupPath + "/Treinamento/TreinamentoLabels.txt", labels.ToArray()[i - 1] + "%");
                }

                MessageBox.Show(textBox1.Text + "foi adicionado!", "Treinamento", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Fazer a detecção primeiro!", "Treinamento falhou!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        void FrameGrabber(object sender, EventArgs e)
        {
            label3.Text = "0";
            //label4.Text = "";
            NomePessoas.Add("");


            //Get the current frame form capture device
            Frame = camera.QueryFrame().Resize(500, 360, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

            //Convert it to Grayscale
            gray = Frame.Convert<Gray, Byte>();

            //Face Detector
            MCvAvgComp[][] facesDetectadas = gray.DetectHaarCascade(
          face,
          1.2,
          5,
          Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
          new Size(20, 20));

            //Action for each element detected
            foreach (MCvAvgComp f in facesDetectadas[0])
            {
                t = t + 1;
                resultado = Frame.Copy(f.rect).Convert<Gray, byte>().Resize(120, 120, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                //draw the face detected in the 0th (gray) channel with blue color
                Frame.Draw(f.rect, new Bgr(Color.Red), 2);


                if (treinaImagens.ToArray().Length != 0)
                {
                    //TermCriteria for face recognition with numbers of trained images like maxIteration
                    MCvTermCriteria termCrit = new MCvTermCriteria(ContTreina, 0.001);

                    //Eigen face recognizer
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(
                       treinaImagens.ToArray(),
                       labels.ToArray(),
                       3000,
                       ref termCrit);

                    nome = recognizer.Recognize(resultado);

                    //Draw the label for each face detected and recognized
                    Frame.Draw(nome, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

                }

                NomePessoas[t - 1] = nome;
                NomePessoas.Add("");


                //Set the number of faces detected on the scene
                label3.Text = facesDetectadas[0].Length.ToString();
            }
            t = 0;

            //Names concatenation of persons recognized
            for (int nfaces = 0; nfaces < facesDetectadas[0].Length; nfaces++)
            {
                nomes = nomes + NomePessoas[nfaces] + ", ";
            }
            //Show the faces procesed and recognizedF
            imageBoxFrameGrabber.Image = Frame;
            label4.Text = nomes;
            nomes = "";
            //Clear the list(vector) of names
            NomePessoas.Clear();
        }
    }
}