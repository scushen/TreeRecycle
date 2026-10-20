using System;
using System.Linq;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public partial class Prices : ClubPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            litFee.Text = Money(Settings.PlatformFeePerTree);
            if (!IsPostBack)
                Bind();
        }

        protected string CustomerPays(decimal price)
        {
            return Money(price + Settings.PlatformFeePerTree);
        }

        private void Bind()
        {
            rptPrices.DataSource = ClubPrice.ForClub(CurrentClub.ClubId, false);
            rptPrices.DataBind();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            var mine = ClubPrice.ForClub(CurrentClub.ClubId, false).ToDictionary(p => p.ClubPriceId);
            foreach (RepeaterItem item in rptPrices.Items)
            {
                int id = int.Parse(((HiddenField)item.FindControl("hfId")).Value);
                ClubPrice p;
                if (!mine.TryGetValue(id, out p))
                    continue; // not this club's row
                string label = Clean(((TextBox)item.FindControl("txtLabel")).Text, 60);
                int feet, sort;
                decimal price;
                if (label == "" || !int.TryParse(((TextBox)item.FindControl("txtFeet")).Text, out feet)
                    || !TryMoney(((TextBox)item.FindControl("txtPrice")).Text, out price) || price < 1 || price > 500)
                {
                    litMsg.Text = "<div class=\"msg err\">Each size needs a label, a size in feet and a price between €1 and €500.</div>";
                    return;
                }
                int.TryParse(((TextBox)item.FindControl("txtSort")).Text, out sort);
                p.Label = label;
                p.SizeFeet = feet;
                p.Price = Math.Round(price, 2);
                p.SortOrder = sort;
                p.Active = ((CheckBox)item.FindControl("chkActive")).Checked;
                p.UpdatedBy = "CLB";
                p.Update();
            }
            Bind();
            litMsg.Text = "<div class=\"msg ok\">Prices saved.</div>";
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            string label = Clean(txtNewLabel.Text, 60);
            int feet;
            decimal price;
            if (label == "" || !int.TryParse(txtNewFeet.Text, out feet) || !TryMoney(txtNewPrice.Text, out price) || price < 1 || price > 500)
            {
                litMsg.Text = "<div class=\"msg err\">Enter a label, a size in feet and a price between €1 and €500.</div>";
                return;
            }
            int nextSort = ClubPrice.ForClub(CurrentClub.ClubId, false).Select(p => p.SortOrder).DefaultIfEmpty(0).Max() + 1;
            new ClubPrice
            {
                ClubId = CurrentClub.ClubId,
                Label = label,
                SizeFeet = feet,
                Price = Math.Round(price, 2),
                SortOrder = nextSort,
                Active = true,
                CreatedBy = "CLB",
                UpdatedBy = "CLB",
            }.Create();
            txtNewLabel.Text = txtNewFeet.Text = txtNewPrice.Text = "";
            Bind();
            litMsg.Text = "<div class=\"msg ok\">Size added.</div>";
        }
    }
}
