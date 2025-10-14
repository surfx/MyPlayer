namespace MyPlayer
{
    partial class frmMyPlayer
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMyPlayer));
            label1 = new Label();
            txtPathMusicas = new TextBox();
            btnOpenFolderMusics = new Button();
            treeView1 = new TreeView();
            imageList1 = new ImageList(components);
            listView1 = new ListView();
            contextMenuStrip1 = new ContextMenuStrip(components);
            btnRandomizar = new Button();
            btnVoltar = new Button();
            btnPlayPause = new Button();
            btnProximo = new Button();
            lblStatus = new Label();
            progressBar1 = new ProgressBar();
            trackBar1 = new TrackBar();
            pictureBox1 = new PictureBox();
            timer1 = new System.Windows.Forms.Timer(components);
            ((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(75, 25);
            label1.TabIndex = 0;
            label1.Text = "Músicas";
            // 
            // txtPathMusicas
            // 
            txtPathMusicas.Location = new Point(93, 9);
            txtPathMusicas.Name = "txtPathMusicas";
            txtPathMusicas.Size = new Size(968, 31);
            txtPathMusicas.TabIndex = 1;
            // 
            // btnOpenFolderMusics
            // 
            btnOpenFolderMusics.Location = new Point(1067, 9);
            btnOpenFolderMusics.Name = "btnOpenFolderMusics";
            btnOpenFolderMusics.Size = new Size(45, 34);
            btnOpenFolderMusics.TabIndex = 2;
            btnOpenFolderMusics.Text = "...";
            btnOpenFolderMusics.UseVisualStyleBackColor = true;
            btnOpenFolderMusics.Click += btnOpenFolderMusics_Click;
            // 
            // treeView1
            // 
            treeView1.ImageIndex = 0;
            treeView1.ImageList = imageList1;
            treeView1.Location = new Point(12, 56);
            treeView1.Name = "treeView1";
            treeView1.SelectedImageIndex = 0;
            treeView1.Size = new Size(286, 347);
            treeView1.TabIndex = 3;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.SetKeyName(0, "icons8-folder-32.png");
            imageList1.Images.SetKeyName(1, "icons8-open-file-folder-32.png");
            imageList1.Images.SetKeyName(2, "icons8-file-32.png");
            // 
            // listView1
            // 
            listView1.ContextMenuStrip = contextMenuStrip1;
            listView1.LargeImageList = imageList1;
            listView1.Location = new Point(304, 56);
            listView1.Name = "listView1";
            listView1.Size = new Size(808, 347);
            listView1.SmallImageList = imageList1;
            listView1.TabIndex = 4;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.DoubleClick += listView1_DoubleClick;
            listView1.MouseDown += listView1_MouseDown;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(24, 24);
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(61, 4);
            // 
            // btnRandomizar
            // 
            btnRandomizar.Location = new Point(12, 409);
            btnRandomizar.Name = "btnRandomizar";
            btnRandomizar.Size = new Size(63, 34);
            btnRandomizar.TabIndex = 6;
            btnRandomizar.Text = "rnd";
            btnRandomizar.UseVisualStyleBackColor = true;
            btnRandomizar.Click += btnRandomizar_Click;
            // 
            // btnVoltar
            // 
            btnVoltar.Location = new Point(81, 409);
            btnVoltar.Name = "btnVoltar";
            btnVoltar.Size = new Size(41, 34);
            btnVoltar.TabIndex = 7;
            btnVoltar.Text = "|<";
            btnVoltar.UseVisualStyleBackColor = true;
            btnVoltar.Click += btnVoltar_Click;
            // 
            // btnPlayPause
            // 
            btnPlayPause.Location = new Point(128, 409);
            btnPlayPause.Name = "btnPlayPause";
            btnPlayPause.Size = new Size(41, 34);
            btnPlayPause.TabIndex = 8;
            btnPlayPause.Text = ">";
            btnPlayPause.UseVisualStyleBackColor = true;
            btnPlayPause.Click += btnPlayPause_Click;
            // 
            // btnProximo
            // 
            btnProximo.Location = new Point(175, 409);
            btnProximo.Name = "btnProximo";
            btnProximo.Size = new Size(41, 34);
            btnProximo.TabIndex = 9;
            btnProximo.Text = ">|";
            btnProximo.UseVisualStyleBackColor = true;
            btnProximo.Click += btnProximo_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(762, 414);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(60, 25);
            lblStatus.TabIndex = 10;
            lblStatus.Text = "Status";
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(930, 409);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(182, 34);
            progressBar1.TabIndex = 11;
            // 
            // trackBar1
            // 
            trackBar1.Location = new Point(222, 409);
            trackBar1.Name = "trackBar1";
            trackBar1.Size = new Size(505, 69);
            trackBar1.TabIndex = 12;
            trackBar1.Scroll += trackBar1_Scroll;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(12, 459);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(1098, 75);
            pictureBox1.TabIndex = 13;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // timer1
            // 
            timer1.Tick += timer1_Tick;
            // 
            // frmMyPlayer
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1122, 556);
            Controls.Add(pictureBox1);
            Controls.Add(trackBar1);
            Controls.Add(progressBar1);
            Controls.Add(lblStatus);
            Controls.Add(btnProximo);
            Controls.Add(btnPlayPause);
            Controls.Add(btnVoltar);
            Controls.Add(btnRandomizar);
            Controls.Add(listView1);
            Controls.Add(treeView1);
            Controls.Add(btnOpenFolderMusics);
            Controls.Add(txtPathMusicas);
            Controls.Add(label1);
            MaximizeBox = false;
            Name = "frmMyPlayer";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "My Player";
            FormClosing += frmMyPlayer_FormClosing;
            Load += frmMyPlayer_Load;
            ((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtPathMusicas;
        private Button btnOpenFolderMusics;
        private TreeView treeView1;
        private ListView listView1;
        private ImageList imageList1;
        private Button btnRandomizar;
        private Button btnVoltar;
        private Button btnPlayPause;
        private Button btnProximo;
        private Label lblStatus;
        private ProgressBar progressBar1;
        private TrackBar trackBar1;
        private ContextMenuStrip contextMenuStrip1;
        private PictureBox pictureBox1;
        private System.Windows.Forms.Timer timer1;
    }
}
