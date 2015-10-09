﻿using System;
using BasePages;
using BLL;
using Computer = Models.Computer;

namespace views.masters
{
    public partial class ComputerMaster : MasterBaseMaster
    {
        private Computers ComputerBasePage { get; set; }
        public Computer Computer { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            
            ComputerBasePage = (Page as Computers);
            Computer = ComputerBasePage.Computer;
            if (Computer == null) //level 2
            {
                Level2.Visible = false;
                Level3.Visible = false;
            }
            else
            {
                Level1.Visible = false;
                if (Request.QueryString["level"] == "3")
                    Level2.Visible = false;
            }
         
        
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            
            lblTitle.Text = "Delete This Host?";
            Session["direction"] = "delete";
            DisplayConfirm();
        }

        protected void btnDeploy_Click(object sender, EventArgs e)
        {
            var image = BLL.Image.GetImage(Computer.ImageId);
            Session["direction"] = "push";
            lblTitle.Text = "Deploy " + image.Name + " To " + Computer.Name + " ?";
            DisplayConfirm();
        }

        protected void btnUpload_Click(object sender, EventArgs e)
        {
            Session["direction"] = "pull";
            lblTitle.Text = "Upload This Computer?";
            DisplayConfirm();
        }

        protected void buttonConfirm_Click(object sender, EventArgs e)
        {
            var direction = (string) (Session["direction"]);
            Session.Remove("direction");
            switch (direction)
            {
                case "delete":
                    BLL.Computer.DeleteComputer(Computer.Id);
                        Response.Redirect("~/views/computers/search.aspx");
                    break;
                case "push":
                {
                    var image = BLL.Image.GetImage(Computer.ImageId);
                    Session["imageID"] = image.Id;

                    if (BLL.Image.Check_Checksum(image))
                    {
                        BLL.Computer.StartUnicast(Computer,direction);                      
                    }
                    else
                    {
                        lblIncorrectChecksum.Text =
                            "This Image Has Not Been Confirmed And Cannot Be Deployed.  <br>Confirm It Now?";
                        DisplayIncorrectChecksum();
                    }
                }
                    break;
                case "pull":
                {
                    BLL.Computer.StartUnicast(Computer, direction);
                }
                    break;
            }
        }

        protected void buttonConfirmChecksum_Click(object sender, EventArgs e)
        {
            ApproveChecksumRedirect();
        }
    }
}