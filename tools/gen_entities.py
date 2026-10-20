"""
Generates the TreeRecycle schema script plus the Castle ActiveRecord DAO and Proxy classes
from one table spec, in the same shape MyGeneration produces for KennelBooker
(GenericNHibernateDAO<T> DAO + wrapper proxy + hand-written partial).

Re-run after changing TABLES:   python -I tools/gen_entities.py
Writes:
  SQL/01_create_tables.sql
  TreeRecycle_DataAccessObjects/TR/Generated/DAO/<Name>DAO.cs
  TreeRecycle_DataAccessObjects/TR/Generated/Proxy/<Name>.cs
Hand-written partials in TreeRecycle_DataAccessObjects/TR/<Name>.cs are never touched.

Column spec: (column, sql_type, default)  - every column is NOT NULL unless the type ends in ' null'.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NS = "TreeRecycle.DataAccessObjects"

# Every table lives in the shared KennelBooker database, so all object names carry this prefix:
# TreeRecycle_club_tbl, PK_TreeRecycle_club_tbl, DF_TreeRecycle_club_tbl_Slug... Columns (incl. PKs) are unprefixed.
PREFIX = "TreeRecycle_"

AUDIT = [
    ("created_date", "datetime", "getutcdate()"),
    ("updated_date", "datetime", "getutcdate()"),
    ("created_by", "varchar(3)", "''"),
    ("updated_by", "varchar(3)", "''"),
]

ADDRESS = [
    ("address_line1", "nvarchar(150)", "''"),
    ("address_line2", "nvarchar(150)", "''"),
    ("town", "nvarchar(100)", "''"),
    ("county", "nvarchar(50)", "''"),
    ("eircode", "varchar(10)", "''"),
    ("latitude", "decimal(9,6)", "0"),
    ("longitude", "decimal(9,6)", "0"),
]

TABLES = [
    ("Club", "club_tbl", [
        ("club_name", "nvarchar(150)", "''"),
        ("slug", "varchar(80)", "''"),
        ("club_type", "nvarchar(50)", "''"),
        ("registration_number", "nvarchar(50)", "''"),
        ("website", "nvarchar(250)", "''"),
        ("contact_name", "nvarchar(100)", "''"),
        ("contact_email", "nvarchar(200)", "''"),
        ("contact_phone", "nvarchar(30)", "''"),
        *ADDRESS,
        ("geocoded", "bit", "0"),
        ("collection_radius_km", "int", "20"),
        ("public_description", "nvarchar(2000)", "''"),
        ("collection_info", "nvarchar(2000)", "''"),
        ("trailer_capacity", "int", "25"),
        ("status", "int", "0"),
        ("status_reason", "nvarchar(500)", "''"),
        ("admin_notes", "nvarchar(2000)", "''"),
        ("approved_date", "datetime null", None),
        ("accepting_orders", "bit", "0"),
        ("orders_close_date", "datetime null", None),
        ("password_hash", "varchar(200)", "''"),
        ("reset_token", "varchar(100)", "''"),
        ("reset_token_expiry", "datetime null", None),
        ("last_login_date", "datetime null", None),
        ("failed_login_count", "int", "0"),
        ("stripe_account_id", "varchar(100)", "''"),
        ("stripe_charges_enabled", "bit", "0"),
        ("stripe_details_submitted", "bit", "0"),
    ], [
        "CREATE UNIQUE NONCLUSTERED INDEX UQ_club_tbl_Slug ON dbo.club_tbl (slug);",
        "CREATE UNIQUE NONCLUSTERED INDEX UQ_club_tbl_ContactEmail ON dbo.club_tbl (contact_email);",
        "CREATE NONCLUSTERED INDEX IX_club_tbl_StripeAccountId ON dbo.club_tbl (stripe_account_id);",
        "CREATE NONCLUSTERED INDEX IX_club_tbl_StatusCounty ON dbo.club_tbl (status, county);",
    ]),
    ("ClubPrice", "club_price_tbl", [
        ("club_id", "int", "0"),
        ("label", "nvarchar(60)", "''"),
        ("size_feet", "int", "0"),
        ("price", "decimal(10,2)", "0"),
        ("sort_order", "int", "0"),
        ("active", "bit", "1"),
    ], [
        "CREATE NONCLUSTERED INDEX IX_club_price_tbl_ClubId ON dbo.club_price_tbl (club_id);",
    ]),
    ("ClubCollectionDate", "club_collection_date_tbl", [
        ("club_id", "int", "0"),
        ("collection_date", "datetime", "getutcdate()"),
        ("label", "nvarchar(100)", "''"),
        ("max_trees", "int", "0"),
        ("active", "bit", "1"),
        ("reminder_sent_date", "datetime null", None),
    ], [
        "CREATE NONCLUSTERED INDEX IX_club_collection_date_tbl_ClubId ON dbo.club_collection_date_tbl (club_id, collection_date);",
    ]),
    ("TreeOrder", "tree_order_tbl", [
        ("order_ref", "varchar(12)", "''"),
        ("club_id", "int", "0"),
        ("club_price_id", "int", "0"),
        ("club_collection_date_id", "int", "0"),
        ("size_label", "nvarchar(60)", "''"),
        ("quantity", "int", "1"),
        ("unit_price", "decimal(10,2)", "0"),
        ("platform_fee_per_tree", "decimal(10,2)", "0"),
        ("club_amount", "decimal(10,2)", "0"),
        ("platform_fee", "decimal(10,2)", "0"),
        ("total_amount", "decimal(10,2)", "0"),
        ("stripe_fee", "decimal(10,2)", "0"),
        ("currency", "varchar(3)", "'eur'"),
        ("customer_name", "nvarchar(100)", "''"),
        ("customer_email", "nvarchar(200)", "''"),
        ("customer_phone", "nvarchar(30)", "''"),
        *ADDRESS,
        ("distance_km", "decimal(6,2)", "0"),
        ("location_notes", "nvarchar(500)", "''"),
        ("status", "int", "0"),
        ("status_note", "nvarchar(500)", "''"),
        ("stripe_checkout_session_id", "varchar(255)", "''"),
        ("stripe_payment_intent_id", "varchar(255)", "''"),
        ("paid_date", "datetime null", None),
        ("collected_date", "datetime null", None),
        ("confirmation_sent_date", "datetime null", None),
        ("reminder_sent_date", "datetime null", None),
        ("run_number", "int", "0"),
        ("run_sequence", "int", "0"),
    ], [
        "CREATE UNIQUE NONCLUSTERED INDEX UQ_tree_order_tbl_OrderRef ON dbo.tree_order_tbl (order_ref);",
        "CREATE NONCLUSTERED INDEX IX_tree_order_tbl_ClubStatus ON dbo.tree_order_tbl (club_id, status, club_collection_date_id);",
        "CREATE NONCLUSTERED INDEX IX_tree_order_tbl_CheckoutSession ON dbo.tree_order_tbl (stripe_checkout_session_id);",
        "CREATE NONCLUSTERED INDEX IX_tree_order_tbl_PaymentIntent ON dbo.tree_order_tbl (stripe_payment_intent_id);",
    ]),
    ("EmailTemplate", "email_template_tbl", [
        ("club_id", "int", "0"),
        ("template_type", "int", "0"),
        ("subject", "nvarchar(200)", "''"),
        ("body", "nvarchar(max)", "''"),
    ], [
        "CREATE UNIQUE NONCLUSTERED INDEX UQ_email_template_tbl_ClubType ON dbo.email_template_tbl (club_id, template_type);",
    ]),
    ("EmailLog", "email_log_tbl", [
        ("club_id", "int", "0"),
        ("tree_order_id", "int", "0"),
        ("template_type", "int", "0"),
        ("to_email", "nvarchar(200)", "''"),
        ("subject", "nvarchar(200)", "''"),
        ("success", "bit", "0"),
        ("provider_message_id", "varchar(100)", "''"),
        ("error_message", "nvarchar(1000)", "''"),
    ], [
        "CREATE NONCLUSTERED INDEX IX_email_log_tbl_TreeOrderId ON dbo.email_log_tbl (tree_order_id);",
    ]),
    ("ClubExpense", "club_expense_tbl", [
        ("club_id", "int", "0"),
        ("description", "nvarchar(200)", "''"),
        ("amount", "decimal(10,2)", "0"),
    ], [
        "CREATE NONCLUSTERED INDEX IX_club_expense_tbl_ClubId ON dbo.club_expense_tbl (club_id);",
    ]),
    ("StripeEvent", "stripe_event_tbl", [
        ("event_id", "varchar(100)", "''"),
        ("event_type", "varchar(100)", "''"),
        ("stripe_account_id", "varchar(100)", "''"),
        ("processed", "bit", "0"),
        ("error_message", "nvarchar(1000)", "''"),
    ], [
        "CREATE UNIQUE NONCLUSTERED INDEX UQ_stripe_event_tbl_EventId ON dbo.stripe_event_tbl (event_id);",
    ]),
]


def pascal(col):
    return "".join(p[:1].upper() + p[1:] for p in col.split("_"))


def camel(col):
    p = pascal(col)
    return p[:1].lower() + p[1:]


def parse_type(sql):
    nullable = sql.endswith(" null")
    base = sql[:-5] if nullable else sql
    m = re.match(r"(\w+)(?:\((\w+)(?:,\d+)?\))?", base)
    name, length = m.group(1), m.group(2)
    if name in ("int",):
        cs, init = "int", "0"
    elif name == "bit":
        cs, init = "bool", "false"
    elif name == "decimal":
        cs, init = "decimal", "0m"
    elif name in ("nvarchar", "varchar"):
        cs, init = "string", "String.Empty"
    elif name == "datetime":
        cs, init = ("Nullable<DateTime>", "new Nullable<DateTime>()") if nullable else ("DateTime", "DateTime.UtcNow")
    else:
        raise ValueError(sql)
    ln = int(length) if length and length.isdigit() else 0
    return base, name, cs, init, nullable, ln


def columns(table, cols):
    pk = table[:-4] + "_id" if table.endswith("_tbl") else table + "_id"
    return pk, list(cols) + AUDIT


def sql_for(name, table, cols, extra):
    pk, allcols = columns(table, cols)
    full = PREFIX + table
    lines = [f"/* {name} */",
             f"IF OBJECT_ID('dbo.{full}', 'U') IS NULL",
             "BEGIN",
             f"CREATE TABLE dbo.{full} (",
             f"    {pk:<28} INT IDENTITY(1,1) NOT NULL,"]
    for col, typ, default in allcols:
        base, _, _, _, nullable, _ = parse_type(typ)
        ddl = f"    {col:<28} {base.upper()} {'NULL' if nullable else 'NOT NULL'}"
        if default is not None:
            ddl += f" CONSTRAINT DF_{full}_{pascal(col)} DEFAULT ({default})"
        lines.append(ddl + ",")
    lines.append(f"    CONSTRAINT PK_{full} PRIMARY KEY CLUSTERED ({pk})")
    lines.append(");")
    lines += [e.replace(f"dbo.{table} ", f"dbo.{full} ").replace(f"_{table}_", f"_{full}_") for e in extra]
    lines.append("END")
    lines.append("GO")
    lines.append("")
    return "\n".join(lines)


def dao_for(name, table, cols):
    pk, allcols = columns(table, cols)
    table = PREFIX + table
    allc = [(pk, "int", None)] + allcols
    parsed = [(c,) + parse_type(t) for c, t, _ in allc]
    o = []
    o.append("using System;\nusing System.Collections;\nusing System.Collections.Generic;\nusing System.Text;\n"
             "using Castle.ActiveRecord;\nusing Castle.ActiveRecord.Queries;\nusing NHibernate;\n"
             "using NHibernate.Criterion;\nusing Castle.ActiveRecord.Framework;\nusing System.Data;\n\n")
    o.append(f"#region '{table}' Schema\n/*\n * '{name}DAO' class maps to '{table}' table.\n")
    for c, base, tname, cs, init, nullable, ln in parsed:
        o.append(f" * \t {c:<24}| Type:{tname:<9}| Len:{ln:<6}| Nullable:{'T' if nullable else 'F'} | PK:{'T' if c == pk else 'F'} | FK:F\n")
    o.append(" */\n#endregion\n/// <summary>\n///\tGenerated by tools/gen_entities.py in the MyGeneration ActiveRecord Object Mapper shape\n/// </summary>\n")
    o.append(f"namespace {NS}\n{{\n\t[Serializable , ActiveRecord(\"{table}\", DynamicUpdate = true, Lazy = false)]\n")
    o.append(f"\tpublic partial class {name}DAO : GenericNHibernateDAO<{name}DAO>\n\t{{\n\n\t\t#region Private Members\n\n")
    for c, base, tname, cs, init, nullable, ln in parsed:
        o.append(f"\t\tprivate {cs} _{camel(c)}; \n")
    o.append("\t\t\n\t\t#endregion\n\n\t\t#region Constuctor(s)\n\t\t\n")
    o.append(f"\t\tpublic {name}DAO()\n\t\t{{\n")
    for c, base, tname, cs, init, nullable, ln in parsed:
        o.append(f"\t\t\t_{camel(c)} = {'-1' if c == pk else init}; \n")
    o.append("\n\t\t}\n\n\t\t#endregion // End of Class Constuctor(s)\n\t\t\n\t\t#region Public Properties\n\t\t\t\n")
    for c, base, tname, cs, init, nullable, ln in parsed:
        o.append("\t\t/// <summary>\n\t\t/// \n\t\t/// </summary>\n")
        if c == pk:
            o.append(f"\t\t[PrimaryKey(PrimaryKeyType.Identity, \"{c}\")]\n")
        else:
            attrs = [f'Column="{c}"']
            if not nullable:
                attrs.append("NotNull=true")
            if ln:
                attrs.append(f"Length={ln}")
            if tname == "nvarchar" and base.endswith("(max)"):
                attrs.append('ColumnType="StringClob"')
            o.append(f"\t\t[Property({', '.join(attrs)})]\n")
        o.append(f"\t\tpublic virtual {cs} {pascal(c)}\n\t\t{{\n\t\t\tget {{ return _{camel(c)}; }}\n\t\t\tset {{ _{camel(c)} = value; }}\n\t\t}}\n\n")
    o.append("\n\t\t#endregion \n\n\t\t#region Table Properties\n\t\t\n        internal protected class PropertyNames\n        {\n")
    for c, *_ in parsed:
        o.append(f"\t\t\tpublic const string {pascal(c)} = \"{pascal(c)}\";\n")
    o.append("\n\t\t}\n\t\t#endregion //Table Properties\n\n\t\t#region Equals, HashCode and ToString overrides\n\t\t\n")
    o.append(f"\t\tpublic override bool Equals( object obj )\n\t\t{{\n\t\t\tif( this == obj ) return true;\n"
             f"\t\t\tif( ( obj == null ) || ( obj.GetType() != this.GetType() ) ) return false;\n"
             f"\t\t\t{name}DAO castObj = ({name}DAO)obj; \n\t\t\treturn ( castObj != null ) &&\n"
             f"\t\t\t\t( this._{camel(pk)} == castObj.{pascal(pk)} );\n\t\t}}\n\t\t\n")
    o.append(f"\t\tpublic override int GetHashCode()\n\t\t{{\n\t\t\tint hash = 57; \n\t\t\thash = 27 * hash * _{camel(pk)}.GetHashCode();\n\t\t\treturn hash; \n\t\t}}\n\t\t\n")
    o.append("\t\tpublic override String ToString()\n        {\n            StringBuilder sbuffer = new StringBuilder();\n\t\t\tsbuffer.Append(\"{\");\n")
    for c, *_ in parsed:
        o.append(f"\t\t\tsbuffer.AppendFormat(\"{pascal(c)} = {{0}}, \",_{camel(c)});\n")
    o.append("\t\t\tsbuffer.Append(\" }\");\n\t\t\treturn sbuffer.ToString();\n        }\n\t\t\n\t\t#endregion\n\t}\n}\n")
    return "".join(o)


def proxy_for(name, table, cols):
    pk, allcols = columns(table, cols)
    allc = [(pk, "int", None)] + allcols
    parsed = [(c,) + parse_type(t) for c, t, _ in allc]
    has_created = any(c == "created_date" for c, *_ in parsed)
    o = []
    o.append("using System;\nusing System.Collections;\nusing System.Collections.Generic;\nusing System.Text;\n"
             "using Castle.ActiveRecord.Queries;\nusing Castle.ActiveRecord;\nusing NHibernate.Criterion;\nusing NHibernate;\n\n\n")
    o.append("/// <summary>\n///\tGenerated by tools/gen_entities.py in the EI ActiveRecord ProxyStub shape\n/// </summary>\n")
    o.append(f"namespace {NS}\n{{\n\t[Serializable]\n\tpublic partial class {name}\n\t{{\n\n\t\t#region Private Members\n"
             f"\t\t\tprivate {name}DAO _baseClass = new {name}DAO();\n\t\t#endregion\n\n\t\t#region Constuctor(s)\n\t\t\n"
             f"\t\tpublic {name}()\n\t\t{{ }}\n\t\t\n\t\tpublic {name}({name}DAO row)\n\t\t{{ \n\t\t\t_baseClass = row;  \n\t\t}}\n\n"
             "\t\t#endregion // End of Class Constuctor(s)\n\t\t\n\t\t#region Public Properties\n\t\t\t\n")
    for c, base, tname, cs, init, nullable, ln in parsed:
        o.append(f"\t\tpublic virtual {cs} {pascal(c)}\n\t\t{{\n\t\t\tget {{ return _baseClass.{pascal(c)}; }}\n\t\t\tset {{ _baseClass.{pascal(c)} = value; }}\n\t\t}}\n\n")
    o.append("\n\t\t#endregion \n\n\t\t#region Table Properties\n\t\t\n        public class PropertyNames\n        {\n")
    for c, *_ in parsed:
        o.append(f"\t\t\tpublic const string {pascal(c)} = \"{pascal(c)}\";\n")
    o.append("\n\t\t}\n\t\t#endregion //Table Properties\n\t\t\n\t\t#region Public Methods\n\n")
    o.append("\t\tpublic void Create()\n        {\n")
    if has_created:
        o.append("            CreatedDate = DateTime.UtcNow;\n            UpdatedDate = DateTime.UtcNow;\n")
    o.append("            _baseClass.Create();\n        }\n\n        public void Update() \n        {\n")
    if has_created:
        o.append("            UpdatedDate = DateTime.UtcNow;\n")
    o.append("            _baseClass.Update();\n        }\n\n        public void Delete() \n        {\n            _baseClass.Delete();\n        }\n\n"
             "\t\t#endregion //Public Methods\n\n\t\t#region Public Standard Search Methods\n\n")
    n = name
    o.append(f"""        public static {n} FindByPrimaryKey(object key)
        {{
            return new {n}({n}DAO.FindByPrimaryKey(key));
        }}

		public static {n} FindByPrimaryKey(object value, bool throwOnNotFound)
        {{
            {n}DAO row = {n}DAO.FindByPrimaryKey(value, throwOnNotFound);
            return row == null ? null : new {n}(row);
        }}

		public static IList<{n}> FindAll(ICriterion criteria)
        {{
            IList<{n}> results = new List<{n}>();
            foreach ({n}DAO row in {n}DAO.FindAll(criteria))
                results.Add(new {n}(row));
            return results;
        }}

        public static IList<{n}> FindAll(ICriterion[] criterias)
        {{
            IList<{n}> results = new List<{n}>();
            foreach ({n}DAO row in {n}DAO.FindAll(criterias))
                results.Add(new {n}(row));
            return results;
        }}

        public static IList<{n}> FindAll(ICriterion criteria, Order order)
        {{
            IList<{n}> results = new List<{n}>();
            foreach ({n}DAO row in {n}DAO.FindAll(new ICriterion[] {{ criteria }}, new Order[] {{ order }}))
                results.Add(new {n}(row));
            return results;
        }}

        public static IList<{n}> FindAll(ICriterion[] criterias, Order[] orders)
        {{
            IList<{n}> results = new List<{n}>();
            foreach ({n}DAO row in {n}DAO.FindAll(criterias, orders))
                results.Add(new {n}(row));
            return results;
        }}

		public static IList<{n}> FindTop(int max, ICriterion[] criterias, Order[] orders)
        {{
            IList<{n}> results = new List<{n}>();
            foreach ({n}DAO row in {n}DAO.FindTop(max, criterias, orders))
                results.Add(new {n}(row));
            return results;
        }}

		public static {n} FindFirst(ICriterion[] criterias)
		{{
			{n}DAO row = {n}DAO.FindFirst(criterias);
			return row == null ? null : new {n}(row);
		}}

		public static {n} FindFirst(ICriterion[] criterias, Order[] orders)
		{{
			{n}DAO row = {n}DAO.FindFirst(criterias, orders);
			return row == null ? null : new {n}(row);
		}}

        public static int Count(ICriterion[] criteria)
        {{
            return {n}DAO.Count(criteria);
        }}

        public static bool Exists(params ICriterion[] criteria)
        {{
            return {n}DAO.Exists(criteria);
        }}

        #endregion //Public Standard Search Methods

""")
    o.append("\t\t#region ToString overrides\n\t\t\t\t\n\t\tpublic override String ToString()\n        {\n            return _baseClass.ToString();\n        }\n\t\t\n\t\t#endregion\n\t}\n}\n")
    return "".join(o)


def write(rel, text):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\r\n") as f:
        f.write(text)


def main():
    sql = ["/*", "  TreeRecycle schema - generated by tools/gen_entities.py",
           "  Lives in the KennelBooker database: every table, constraint and index is prefixed TreeRecycle_.",
           "  Conventions: snake_case _tbl tables, NOT NULL with defaults, IDENTITY PKs, no FK constraints.",
           "  Safe to re-run: each table is only created when missing.", "*/", "SET ANSI_NULLS ON;", "SET QUOTED_IDENTIFIER ON;", "GO", ""]
    for name, table, cols, extra in TABLES:
        sql.append(sql_for(name, table, cols, extra))
        write(f"TreeRecycle_DataAccessObjects/TR/Generated/DAO/{name}DAO.cs", dao_for(name, table, cols))
        write(f"TreeRecycle_DataAccessObjects/TR/Generated/Proxy/{name}.cs", proxy_for(name, table, cols))
    write("SQL/01_create_tables.sql", "\n".join(sql))
    print(f"Generated {len(TABLES)} tables")


if __name__ == "__main__":
    sys.exit(main())
