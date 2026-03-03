Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Drawing
Imports System.Windows.Forms
Imports SmartPrep.Modules

Public Class ReviewDirectorForm
    Inherits Form

    Private httpClient As New HttpClient()
    Private documents As New List(Of Document)

    ' UI Controls
    Private cmbDocuments As New ComboBox()
    Private numItems As New NumericUpDown()
    Private cmbDifficulty As New ComboBox()
    Private cmbFocus As New ComboBox()
    Private cmbExamType As New ComboBox()
    Private btnGenerate As New Button()
    Private dgvQuestions As New DataGridView()
    Private lstSections As New CheckedListBox()

    Public Sub New()
        Me.Text = "SmartPrep – Exam Builder"
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)
    
        httpClient.Timeout = TimeSpan.FromMinutes(3)
        BuildUI()
        AddHandler Me.Load, AddressOf MainForm_Load
    End Sub

    Private Sub BuildUI()

        Dim mainLayout As New TableLayoutPanel()
        mainLayout.Dock = DockStyle.Fill
        mainLayout.RowCount = 4
        mainLayout.ColumnCount = 1
        mainLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        mainLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        mainLayout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        ' ===== MATERIAL PANEL =====
        Dim pnlMaterial As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(10)}

        Dim lblMaterial As New Label() With {
            .Text = "Material",
            .Dock = DockStyle.Top,
            .Height = 25
        }

        cmbDocuments.Dock = DockStyle.Top
        cmbDocuments.DropDownStyle = ComboBoxStyle.DropDownList

        pnlMaterial.Controls.Add(cmbDocuments)
        pnlMaterial.Controls.Add(lblMaterial)


        ' ===== CONFIG PANEL =====
        Dim pnlConfig As New FlowLayoutPanel()
        pnlConfig.Dock = DockStyle.Fill
        pnlConfig.Padding = New Padding(10)
        pnlConfig.AutoSize = True

        ' Items
        pnlConfig.Controls.Add(New Label() With {.Text = "Items:", .AutoSize = True})
        numItems.Minimum = 10
        numItems.Maximum = 200
        numItems.Value = 20
        pnlConfig.Controls.Add(numItems)

        ' Difficulty
        pnlConfig.Controls.Add(New Label() With {.Text = "Difficulty:", .AutoSize = True})
        cmbDifficulty.DropDownStyle = ComboBoxStyle.DropDownList
        cmbDifficulty.Items.AddRange(New String() {"Quiz", "Long Exam", "Midterm", "Final", "Board Review"})
        cmbDifficulty.SelectedIndex = 0
        pnlConfig.Controls.Add(cmbDifficulty)

        ' Focus
        pnlConfig.Controls.Add(New Label() With {.Text = "Focus:", .AutoSize = True})
        cmbFocus.DropDownStyle = ComboBoxStyle.DropDownList
        cmbFocus.Items.AddRange(New String() {"Comprehensive", "Weak Areas", "Specific Topic"})
        cmbFocus.SelectedIndex = 0
        AddHandler cmbFocus.SelectedIndexChanged, AddressOf cmbFocus_Changed
        pnlConfig.Controls.Add(cmbFocus)

        lstSections.Visible = False
        lstSections.Height = 120
        lstSections.CheckOnClick = True
        pnlConfig.Controls.Add(lstSections)

        ' Exam Type
        pnlConfig.Controls.Add(New Label() With {.Text = "Exam Type:", .AutoSize = True})
        cmbExamType.DropDownStyle = ComboBoxStyle.DropDownList
        cmbExamType.Items.AddRange(New String() {"Standard", "Multiple Choice", "Mixed"})
        cmbExamType.SelectedIndex = 0
        pnlConfig.Controls.Add(cmbExamType)

        ' ===== BUTTON PANEL =====
        Dim pnlButton As New Panel() With {.Dock = DockStyle.Fill, .Height = 60}

        btnGenerate.Text = "Generate Exam"
        btnGenerate.Height = 40
        btnGenerate.Width = 200
        btnGenerate.BackColor = Color.FromArgb(25, 118, 210)
        btnGenerate.ForeColor = Color.White
        btnGenerate.FlatStyle = FlatStyle.Flat
        btnGenerate.FlatAppearance.BorderSize = 0
        btnGenerate.Cursor = Cursors.Hand
        btnGenerate.Anchor = AnchorStyles.None

        AddHandler btnGenerate.Click, AddressOf btnGenerate_Click

        pnlButton.Controls.Add(btnGenerate)
        btnGenerate.Left = (pnlButton.Width - btnGenerate.Width) \ 2
        btnGenerate.Top = 10

        ' ===== DATAGRID =====
        dgvQuestions.Dock = DockStyle.Fill
        dgvQuestions.ReadOnly = True
        dgvQuestions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvQuestions.AllowUserToAddRows = False
        dgvQuestions.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvQuestions.RowHeadersVisible = False

        ' ===== ADD TO MAIN LAYOUT =====
        mainLayout.Controls.Add(pnlMaterial, 0, 0)
        mainLayout.Controls.Add(pnlConfig, 0, 1)
        mainLayout.Controls.Add(pnlButton, 0, 2)
        mainLayout.Controls.Add(dgvQuestions, 0, 3)

        Me.Controls.Add(mainLayout)
    End Sub

    Private Async Sub MainForm_Load(sender As Object, e As EventArgs)
        Await LoadDocuments()
    End Sub

    Private Async Function LoadDocuments() As Task
        Try
            Dim response = Await httpClient.GetStringAsync("http://localhost:8000/materials")

            Dim options As New JsonSerializerOptions With {
                .PropertyNameCaseInsensitive = True
            }

            documents = JsonSerializer.Deserialize(Of List(Of Document))(response, options)

            cmbDocuments.DataSource = documents
            cmbDocuments.DisplayMember = "title_content"
            cmbDocuments.ValueMember = "id"

        Catch ex As Exception
            MessageBox.Show("Failed to load materials: " & ex.Message)
        End Try
    End Function

    Private Async Function LoadSections() As Task
        If cmbDocuments.SelectedItem Is Nothing Then Return

        Dim selectedDoc = CType(cmbDocuments.SelectedItem, Document)

        Dim response = Await httpClient.GetStringAsync(
            $"http://localhost:8000/sections/{selectedDoc.id}"
        )

        Dim options As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True
        }

        Dim sections = JsonSerializer.Deserialize(Of List(Of Section))(response, options)

        lstSections.Items.Clear()

        For Each s In sections
            lstSections.Items.Add(s.section_name)
        Next

        AdjustSectionListWidth()
    End Function

    Private Sub AdjustSectionListWidth()
        Dim maxWidth As Integer = 0

        Using g As Graphics = lstSections.CreateGraphics()
            For Each item In lstSections.Items
                Dim textSize = g.MeasureString(item.ToString(), lstSections.Font)
                If textSize.Width > maxWidth Then
                    maxWidth = CInt(textSize.Width)
                End If
            Next
        End Using

        ' Add padding
        lstSections.Width = maxWidth + 40

    End Sub

    Private Sub DisplayQuestions(questions As List(Of ExamQuestion), examType As String)

        dgvQuestions.DataSource = Nothing
        dgvQuestions.Columns.Clear()

        ' Question column
        dgvQuestions.Columns.Add("Question", "Question")
        dgvQuestions.Columns("Question").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

        ' Choices column
        If examType = "Multiple Choice" Or examType = "Mixed" Then
            dgvQuestions.Columns.Add("Choices", "Choices")
            dgvQuestions.Columns("Choices").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvQuestions.Columns("Choices").DefaultCellStyle.NullValue = "(N/A)"
        End If

        ' Answer column
        dgvQuestions.Columns.Add("Answer", "Answer")
        dgvQuestions.Columns("Answer").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

        ' Populate rows
        For Each q In questions
            Dim choiceText As String = Nothing
            If q.choices IsNot Nothing AndAlso (examType = "Multiple Choice" Or examType = "Mixed") Then
                choiceText = String.Join(vbCrLf, q.choices.Select(Function(c, i) $"{Chr(65 + i)}. {c}"))
            End If

            Dim answerText As String = If(String.IsNullOrEmpty(q.correct_answer), "(Short Answer)", q.correct_answer)

            If examType = "Multiple Choice" Or examType = "Mixed" Then
                dgvQuestions.Rows.Add(q.question_text, choiceText, answerText)
            Else
                dgvQuestions.Rows.Add(q.question_text, answerText) ' Only two columns exist
            End If
        Next

    End Sub

    Private Async Sub cmbFocus_Changed(sender As Object, e As EventArgs)

        If cmbFocus.Text = "Specific Topic" Then
            lstSections.Visible = True
            Await LoadSections()
        Else
            lstSections.Visible = False
        End If

    End Sub

    Private Async Sub btnGenerate_Click(sender As Object, e As EventArgs)

        If cmbDocuments.SelectedItem Is Nothing Then
            MessageBox.Show("Please select a material.")
            Return
        End If

        Dim selectedDoc = CType(cmbDocuments.SelectedItem, Document)

        Dim selectedSections As New List(Of String)

        If cmbFocus.Text = "Specific Topic" Then
            For Each item In lstSections.CheckedItems
                selectedSections.Add(item.ToString())
            Next
        End If

        Dim payload = New With {
            .document_id = selectedDoc.id,
            .items = CInt(numItems.Value),
            .difficulty = cmbDifficulty.Text,
            .focus = cmbFocus.Text,
            .exam_type = cmbExamType.Text,
            .section_names = If(selectedSections.Count > 0, selectedSections, Nothing)
        }

        Dim json = JsonSerializer.Serialize(payload)
        Dim content = New StringContent(json, Encoding.UTF8, "application/json")

        Try
            btnGenerate.Enabled = False
            btnGenerate.Text = "Generating..."
            
            Dim response = Await httpClient.PostAsync("http://localhost:8000/generate-exam", content)
            Dim result = Await response.Content.ReadAsStringAsync()

            Dim options As New JsonSerializerOptions With {
                .PropertyNameCaseInsensitive = True
            }

            Dim questions = JsonSerializer.Deserialize(Of List(Of ExamQuestion))(result, options)

            DisplayQuestions(questions, cmbExamType.Text)
        Catch ex As Exception
            MessageBox.Show("Generation failed: " & ex.Message)
        Finally
            btnGenerate.Enabled = True
            btnGenerate.Text = "Generate Exam"
        End Try

    End Sub

End Class