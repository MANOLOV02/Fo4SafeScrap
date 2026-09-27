Option Strict On
Option Infer On

Namespace UI

''' <summary>What each tab is, how a result comes out (the law of <see cref="Engine.Evaluator"/>), what the warning is and
''' what to do.</summary>
Partial Class HowItWorksDialog

    Friend Sub New()
        InitializeComponent()
        Fill()
    End Sub

    Private Sub Fill()
        Dim t = TextHelp
        t.Clear()
        Heading(t, "What SafeScrap does")
        Body(t, "The workshop's recipes say which objects of a settlement can be scrapped: its scrap recipes, and its build " &
                "recipes (what the workshop can build, it can also scrap — a bathtub, a bed). Many of them are not junk: walls, " &
                "floors, trees, rocks, ground. SafeScrap builds, from your load order, the list of every object those recipes " &
                "cover and helps you decide which ones are SAFE junk. That list is what the F4SE plugin will scrap.")
        Heading(t, "The three tabs")
        Body(t, "Objects — every base object a recipe covers, with its result (Yes = safe junk, No = keep, Review = not decided).")
        Body(t, "Groups — the recipes (Kind: Scrap or Build). A decision on a group applies to all its objects.")
        Body(t, "Folders — where each object's mesh is filed (setdressing\rubble, architecture\…). The folder tells what the object is. " &
                "A decision on a folder applies to the objects filed there.")
        Heading(t, "How a result comes out")
        Body(t, "The first level that decides wins:")
        Body(t, "  1. Your decision on the object.")
        Body(t, "  2. Your decisions on the groups that contain it. When they disagree, No wins.")
        Body(t, "  3. Its type rule (Rules… → Type rules), e.g. CONT = No: containers hold items. It is a default: it does not apply when you decided one of its folders.")
        Body(t, "  4. Its folder: your decision on the folder, or else the folder rule (Rules…). A rule for a folder also " &
                "covers the folders under it; the most specific rule wins. A folder without a rule says Review.")
        Body(t, "     A static collection (several pieces merged) takes the folders of its pieces: any No ⇒ No; all Yes ⇒ Yes; otherwise Review.")
        Body(t, "  5. Warning: if the folders say Yes but the EditorID contains a word of a plant, terrain or a building part " &
                "(Rules… → Warning words), the result is lowered to Review so you look at it.")
        Body(t, "The panel ""Why this result"" shows this chain for the selected object.")
        Heading(t, "The Decision box")
        Body(t, "Checked = Yes · Empty = No · Filled square = Undecided (the levels below decide).")
        Body(t, "Click the box or press Space to go Undecided → Yes → No → Undecided. Y, N and U set Yes, No or Undecided. " &
                "With several rows selected, the value goes to all of them (for a click: the next value of the clicked row).")
        Heading(t, "What to do")
        Body(t, "  1. Show: Review. These are the objects nobody decided.")
        Body(t, "  2. Decide where it is cheapest: a folder or a group settles many objects at once; an object settles just itself. " &
                "Use the preview to see what it is.")
        Body(t, "  3. Check Show: With warning (objects the warning lowered to Review, and those that are Yes although the name suggests a plant, terrain or a building part), and the Yes list, for anything that is not junk.")
        Body(t, "  4. Save (Ctrl+S). Your decisions are kept by plugin and ID, so they survive load-order changes.")
        Body(t, "  5. Export to game: saves your decisions, writes the list to Data\F4SE\Plugins\SafeScrap.txt and installs the F4SE plugin next to it " &
                "(the plugin comes in a later version; until then only the list is written). With nothing in Yes the list is empty and the plugin scraps nothing.")
        Body(t, "     Remove from game deletes both files; your decisions and rules stay in the app.")
        Heading(t, "Filter by workshop")
        Body(t, "Filter by workshop… reads the objects placed in the world by your load order once (it takes a few seconds) and lists " &
                "every workshop. Pick one to see only the objects inside its build area. Nothing read is kept: only which objects " &
                "are in each workshop.")
        Body(t, "Inside means what the game means: the area boxes linked to the workbench (the build area you see in the workshop), " &
                "or — for a workshop without boxes, such as Home Plate — a radius around it (5000 units, or its location's own radius). " &
                "An object counts when it stands in the same cell (interior) or world (exterior) and its position is inside.")
        Body(t, "The objects are the ones your plugins place, as the LAST plugin that touches each one leaves it: a mod that moves, " &
                "deletes or adds objects, or changes a workshop's area, is taken into account. What you built, scrapped or moved in " &
                "your save game is not seen.")
        Heading(t, "Rules")
        Body(t, "Rules… edits the folder rules and the warning words. Your edits are kept as changes over the app's rules, so a " &
                "newer version of the app still brings its new rules; when the app changes a rule you had edited, Rules… marks it " &
                "and you choose (Keep mine / Use the app's). Restore defaults drops your changes (a backup is kept).")
        t.SelectionStart = 0
    End Sub

    Private Shared Sub Heading(t As RichTextBox, text As String)
        t.SelectionFont = New Font(t.Font.FontFamily, t.Font.Size + 2, FontStyle.Bold)
        t.AppendText(If(t.TextLength > 0, vbLf, "") & text & vbLf)
    End Sub

    Private Shared Sub Body(t As RichTextBox, text As String)
        t.SelectionFont = t.Font
        t.AppendText(text & vbLf)
    End Sub

End Class

End Namespace
