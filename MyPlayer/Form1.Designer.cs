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
            button1 = new Button();
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
            listView1.LargeImageList = imageList1;
            listView1.Location = new Point(304, 56);
            listView1.Name = "listView1";
            listView1.Size = new Size(808, 347);
            listView1.SmallImageList = imageList1;
            listView1.TabIndex = 4;
            listView1.UseCompatibleStateImageBehavior = false;
            // 
            // button1
            // 
            button1.Location = new Point(12, 409);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 5;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // frmMyPlayer
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1122, 469);
            Controls.Add(button1);
            Controls.Add(listView1);
            Controls.Add(treeView1);
            Controls.Add(btnOpenFolderMusics);
            Controls.Add(txtPathMusicas);
            Controls.Add(label1);
            MaximizeBox = false;
            Name = "frmMyPlayer";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "My Player";
            Load += frmMyPlayer_Load;
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
        private Button button1;
    }
}
