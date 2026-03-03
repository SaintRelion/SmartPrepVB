Imports System.Net.Http
Imports System.Text.Json
Imports System.Windows.Forms
Imports SmartPrep.Modules

Public Class ExamListForm
    Inherits Form

    Private httpClient As New HttpClient()
    Private lstExams As New ListBox()
    Private btnOpenExam As New Button()
    Private cmbFocus As New ComboBox()
    Private cmbExamType As New ComboBox()
    Private btnFilter As New Button()

    Public Sub New()
        Me.Text = "Select Exam"
        Me.WindowState = FormWindowState.Normal
        Me.Size = New Size(600, 700)

        ' --- Filter ComboBoxes ---
        cmbFocus.Items.AddRange(New String() {"All", "Comprehensive", "Weak Areas", "Specific Topic"})
        cmbFocus.SelectedIndex = 0
        cmbFocus.Top = 10
        cmbFocus.Left = 10
        cmbFocus.Width = 200
        Me.Controls.Add(cmbFocus)

        cmbExamType.Items.AddRange(New String() {"All", "Standard", "Multiple Choice", "Mixed"})
        cmbExamType.SelectedIndex = 0
        cmbExamType.Top = 10
        cmbExamType.Left = 220
        cmbExamType.Width = 200
        Me.Controls.Add(cmbExamType)

        btnFilter.Text = "Apply Filter"
        btnFilter.Top = 10
        btnFilter.Left = 440
        btnFilter.Width = 120
        AddHandler btnFilter.Click, AddressOf LoadExamsAsync
        Me.Controls.Add(btnFilter)

        ' --- ListBox for exams ---
        lstExams.Top = 50
        lstExams.Left = 10
        lstExams.Width = 560
        lstExams.Height = 550
        Me.Controls.Add(lstExams)

        ' --- Button to open selected exam ---
        btnOpenExam.Text = "Open Exam"
        btnOpenExam.Top = 610
        btnOpenExam.Left = 10
        btnOpenExam.Width = 560
        AddHandler btnOpenExam.Click, AddressOf OpenSelectedExam
        Me.Controls.Add(btnOpenExam)

        ' Load exams on startup
        LoadExamsAsync()
    End Sub

    Private Async Sub LoadExamsAsync()
        Try
            ' Build query string for filters
            Dim url As String = "http://localhost:8000/exams"
            Dim queryParams As New List(Of String)
            If cmbFocus.SelectedItem IsNot Nothing AndAlso cmbFocus.SelectedItem.ToString() <> "All" Then
                queryParams.Add($"focus={cmbFocus.SelectedItem}")
            End If
            If cmbExamType.SelectedItem IsNot Nothing AndAlso cmbExamType.SelectedItem.ToString() <> "All" Then
                queryParams.Add($"exam_type={cmbExamType.SelectedItem}")
            End If
            If queryParams.Count > 0 Then
                url &= "?" & String.Join("&", queryParams)
            End If

            Dim response = Await httpClient.GetStringAsync(url)

            ' Deserialize using System.Text.Json only
            Using doc As JsonDocument = JsonDocument.Parse(response)
                lstExams.Items.Clear()
                For Each elem In doc.RootElement.EnumerateArray()
                    Dim displayText As String = $"{elem.GetProperty("id").GetInt32()} - {elem.GetProperty("focus").GetString()} ({elem.GetProperty("exam_type").GetString()})"
                    lstExams.Items.Add(New ExamItem With {
                        .Id = elem.GetProperty("id").GetInt32(),
                        .DisplayText = displayText
                    })
                Next
            End Using

        Catch ex As Exception
            MessageBox.Show("Failed to load exams: " & ex.Message)
        End Try
    End Sub

    Private Sub OpenSelectedExam(sender As Object, e As EventArgs)
        If lstExams.SelectedItem Is Nothing Then
            MessageBox.Show("Please select an exam first.")
            Return
        End If

        Dim selected As ExamItem = CType(lstExams.SelectedItem, ExamItem)
        Dim reviewForm As New RevieweeForm(selected.Id)
        reviewForm.Show()
    End Sub
End Class