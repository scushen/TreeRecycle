using System;
using System.Web.UI.WebControls;
using TreeRecycle.DataAccessObjects;

namespace TreeRecycle.Web.ClubArea
{
    public partial class MoneyPage : ClubPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                Bind();
        }

        private void Bind()
        {
            ClubMoney m = ClubMoney.For(CurrentClub, TreeOrder.ForClub(CurrentClub.ClubId));
            litTrees.Text = m.Trees.ToString();
            litGross.Text = Money(m.Gross);
            litStripe.Text = Money(m.StripeFees);
            litExpenses.Text = Money(m.Expenses);
            litNet.Text = Money(m.Net);
            litPlatform.Text = Money(m.PlatformFees);
            rptExpenses.DataSource = ClubExpense.ForClub(CurrentClub.ClubId);
            rptExpenses.DataBind();
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            decimal amount;
            string desc = Clean(txtDesc.Text, 200);
            if (desc == "" || !TryMoney(txtAmount.Text, out amount) || amount <= 0)
            {
                litMsg.Text = "<div class=\"msg err\">Enter a description and an amount.</div>";
                return;
            }
            new ClubExpense { ClubId = CurrentClub.ClubId, Description = desc, Amount = Math.Round(amount, 2), CreatedBy = "CLB", UpdatedBy = "CLB" }.Create();
            txtDesc.Text = txtAmount.Text = "";
            litMsg.Text = "";
            Bind();
        }

        protected void rptExpenses_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int id;
            int.TryParse(Convert.ToString(e.CommandArgument), out id);
            ClubExpense x = ClubExpense.FindByPrimaryKey(id, false);
            if (x != null && x.ClubId == CurrentClub.ClubId)
                x.Delete();
            Bind();
        }
    }
}
