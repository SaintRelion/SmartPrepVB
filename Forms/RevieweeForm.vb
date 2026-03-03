Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Drawing
Imports System.Windows.Forms

Public Class RevieweeForm
    Inherits Form

    Private httpClient As New HttpClient()
    Private examId As Integer
    Private questions As List(Of Question)

    ' Panels
    Private pnlExamInfo As New Panel()
    Private pnlQuestions As New Panel()
    Private btnSubmit As New Button()
    Private tbl As TableLayoutPanel

    ' Question class
    Public Class Question
        Public Property id As Integer
        Public Property question_text As String
        Public Property choices As List(Of String)
        Public Property correct_answer As String
        Public Property answer As String ' student's answer
    End Class

    Public Sub New(examId As Integer)
        Me.Text = "Review Exam"
        Me.WindowState = FormWindowState.Maximized
        Me.BackColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        ' Exam info panel (top)
        pnlExamInfo.Dock = DockStyle.Top
        pnlExamInfo.Height = 50
        pnlExamInfo.Padding = New Padding(20)
        Me.Controls.Add(pnlExamInfo)

        ' Scrollable questions panel
        pnlQuestions.Dock = DockStyle.Fill
        pnlQuestions.AutoScroll = True
        pnlQuestions.Padding = New Padding(20, pnlExamInfo.Height + 10, 20, 20) ' <- add top padding
        Me.Controls.Add(pnlQuestions)

        ' Submit button
        btnSubmit.Text = "Submit Answers"
        btnSubmit.Height = 45
        btnSubmit.Width = 200
        btnSubmit.BackColor = Color.FromArgb(25, 118, 210)
        btnSubmit.ForeColor = Color.White
        btnSubmit.FlatStyle = FlatStyle.Flat
        btnSubmit.FlatAppearance.BorderSize = 0
        btnSubmit.Cursor = Cursors.Hand
        AddHandler btnSubmit.Click, Async Sub(sender, e) Await SubmitAnswersAsync()
        btnSubmit.Dock = DockStyle.Bottom
        btnSubmit.Margin = New Padding(0, 10, 0, 10)
        Me.Controls.Add(btnSubmit)

        ' Load exam
        InitializeAsync(examId )
    End Sub

    Private Async Sub InitializeAsync(examId As Integer)
        Await LoadExamAsync(examId )
    End Sub

    ' Safe JSON parse for choices
    Private Function SafeChoices(val As Object) As List(Of String)
        If val Is Nothing Then Return New List(Of String)
        Try
            Dim s = val.ToString()
            Dim parsed = JsonSerializer.Deserialize(Of List(Of String))(s)
            Return If(parsed Is Nothing, New List(Of String), parsed)
        Catch
            Return New List(Of String)
        End Try
    End Function

    ' Load exam
    Public Async Function LoadExamAsync(examId As Integer) As Task
        Me.examId = examId
        Try
            Dim url = $"http://localhost:8000/exams/{examId}"
            Dim response = Await httpClient.GetStringAsync(url)

            Dim options As New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True}

            Dim exam = JsonSerializer.Deserialize(Of Dictionary(Of String, Object))(response, options)
            Dim questionsJson = exam("questions").ToString()
            questions = JsonSerializer.Deserialize(Of List(Of Question))(questionsJson, options)

            ' Display exam info
            pnlExamInfo.Controls.Clear()
            Dim lblInfo As New Label() With {
                .Text = $"Focus: {exam("focus")}    |    Type: {exam("exam_type")}    |    Total Items: {exam("total_items")}",
                .AutoSize = True,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .ForeColor = Color.FromArgb(33, 33, 33)
            }
            pnlExamInfo.Controls.Add(lblInfo)

            ' Clear previous controls
            pnlQuestions.Controls.Clear()

            ' Scrollable panel for all questions
            Dim pnlScroll As New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .Padding = New Padding(0)
            }
            pnlQuestions.Controls.Add(pnlScroll)

            ' TableLayoutPanel for two columns
            tbl = New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .Margin = New Padding(0, 10, 0, 0),
                .ColumnCount = 2
            }

            tbl.ColumnStyles.Clear()
            tbl.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            tbl.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

            Dim rowCount As Integer = Math.Ceiling(questions.Count / 2.0)
            tbl.RowCount = rowCount
            tbl.RowStyles.Clear()
            For i = 0 To rowCount - 1
                tbl.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            Next

            tbl.GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            pnlScroll.Controls.Add(tbl)

            ' Function to create question panel
            Dim CreateQuestionPanel = Function(q As Question, qNum As Integer) As Panel
                Dim pnlQ As New Panel() With {
                    .AutoSize = True,
                    .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    .Padding = New Padding(10),
                    .Margin = New Padding(0, 0, 20, 15),
                    .BackColor = If(qNum Mod 2 = 0, Color.FromArgb(250, 250, 250), Color.FromArgb(245, 245, 245)),
                    .BorderStyle = BorderStyle.FixedSingle
                }

                Dim lbl As New Label() With {
                    .Text = $"{qNum}. {q.question_text}",
                    .Font = New Font("Segoe UI", 11),
                    .AutoSize = False,
                    .MaximumSize = New Size(380, 0),
                    .AutoEllipsis = True
                }
                lbl.Size = New Size(380, lbl.PreferredHeight)
                pnlQ.Controls.Add(lbl)

                Dim controlTop As Integer = lbl.Bottom + 5

                If q.choices IsNot Nothing AndAlso q.choices.Count > 0 Then
                    Dim cmb As New ComboBox() With {
                        .Top = controlTop,
                        .Left = 0,
                        .Width = 380,
                        .DropDownStyle = ComboBoxStyle.DropDownList,
                        .Tag = q.id
                    }
                    cmb.Items.AddRange(q.choices.ToArray())
                    pnlQ.Controls.Add(cmb)
                Else
                    Dim txt As New TextBox() With {
                        .Multiline = True,
                        .Top = controlTop,
                        .Left = 0,
                        .Width = 380,
                        .Height = 80,
                        .Tag = q.id
                    }
                    pnlQ.Controls.Add(txt)
                End If

                Return pnlQ
            End Function

            ' Add questions in column-first order (left: 1–N, right: N+1–end)
            Dim half As Integer = Math.Ceiling(questions.Count / 2.0)

            For i = 0 To half - 1
                ' Left column
                tbl.Controls.Add(CreateQuestionPanel(questions(i), i + 1), 0, i)

                ' Right column (if exists)
                Dim rightIndex = i + half
                If rightIndex < questions.Count Then
                    tbl.Controls.Add(CreateQuestionPanel(questions(rightIndex), rightIndex + 1), 1, i)
                End If
            Next

        Catch ex As Exception
            MessageBox.Show("Failed to load exam: " & ex.Message)
        End Try
    End Function

    ' Submit answers
    Public Async Function SubmitAnswersAsync() As Task
        ' Collect answers
        Dim payload As New List(Of Object)
        For Each pnlQ As Panel In tbl.Controls
            Dim qId As Integer = -1
            Dim answerText As String = ""

            For Each ctl In pnlQ.Controls
                If TypeOf ctl Is TextBox Then
                    qId = CInt(ctl.Tag)
                    answerText = CType(ctl, TextBox).Text
                ElseIf TypeOf ctl Is ComboBox Then
                    qId = CInt(ctl.Tag)
                    answerText = CType(ctl, ComboBox).SelectedItem?.ToString()
                End If
            Next

            answerText = If(answerText, "")

            If qId <> -1 Then
                payload.Add(New With {
                    Key .examination_id = examId,
                    Key .question_id = qId,
                    Key .answer_text = If(answerText, "")
                })
            End If
        Next

        ' Dim jsonDebug As String = System.Text.Json.JsonSerializer.Serialize(payload, New System.Text.Json.JsonSerializerOptions With {.WriteIndented = True})

        ' ' Show payload in a message box
        ' MessageBox.Show("Payload being sent:" & Environment.NewLine & jsonDebug)

        ' Send to backend
        Try
            Dim json As String = System.Text.Json.JsonSerializer.Serialize(payload, New System.Text.Json.JsonSerializerOptions With {.WriteIndented = True})

            ' Send to backend
            Dim url = "http://localhost:8000/answers"
            Dim content = New StringContent(json, Encoding.UTF8, "application/json")
            Dim response = Await httpClient.PostAsync(url, content)
            Dim responseText = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                ' --- Parse response using System.Text.Json ONLY ---
                Using doc As System.Text.Json.JsonDocument = System.Text.Json.JsonDocument.Parse(responseText)
                    Dim root = doc.RootElement

                    Dim scoreMsg As String = ""

                    If root.TryGetProperty("score", Nothing) Then
                        Dim score = root.GetProperty("score")
                        Dim correct As Integer = If(score.TryGetProperty("correct", Nothing), score.GetProperty("correct").GetInt32(), 0)
                        Dim total As Integer = If(score.TryGetProperty("total", Nothing), score.GetProperty("total").GetInt32(), 0)
                        Dim pct As Double = If(score.TryGetProperty("percentage", Nothing), score.GetProperty("percentage").GetDouble(), 0)

                        scoreMsg = $"You answered {correct} out of {total} correctly ({pct:F1}%)."
                    End If

                    MessageBox.Show("Answers submitted successfully!" & Environment.NewLine & scoreMsg)
                End Using
            Else
                MessageBox.Show("Failed to submit answers. Status: " & response.StatusCode & Environment.NewLine & responseText)
            End If

        Catch ex As Exception
            MessageBox.Show("Error submitting answers: " & ex.Message)
        End Try
    End Function
End Class