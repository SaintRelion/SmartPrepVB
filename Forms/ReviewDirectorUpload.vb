Imports System.Net.Http
Imports System.Text.Json
Imports System.IO

Public Class ReviewDirectorUpload
    Inherits Form

    Private httpClient As New HttpClient()

    Private btnSelect As New Button()
    Private btnUpload As New Button()
    Private txtFilePath As New TextBox()
    Private chkUseGPU As New CheckBox()
    Private lblStatus As New Label()

    Private selectedFilePath As String = ""

    Public Sub New()
        Me.Text = "Upload Study Material"
        Me.Size = New Size(600, 250)

        httpClient.Timeout = TimeSpan.FromMinutes(3)

        ' File path textbox
        txtFilePath.Top = 20
        txtFilePath.Left = 20
        txtFilePath.Width = 400
        txtFilePath.ReadOnly = True
        Me.Controls.Add(txtFilePath)

        ' Select button
        btnSelect.Text = "Select PDF"
        btnSelect.Top = 20
        btnSelect.Left = 430
        AddHandler btnSelect.Click, AddressOf SelectFile
        Me.Controls.Add(btnSelect)

        ' ' GPU checkbox
        ' chkUseGPU.Text = "Use GPU (EC2)"
        ' chkUseGPU.Top = 60
        ' chkUseGPU.Left = 20
        ' Me.Controls.Add(chkUseGPU)

        ' Upload button
        btnUpload.Text = "Upload"
        btnUpload.Top = 100
        btnUpload.Left = 20
        AddHandler btnUpload.Click, AddressOf UploadFileAsync
        Me.Controls.Add(btnUpload)

        ' Status label
        lblStatus.Top = 140
        lblStatus.Left = 20
        lblStatus.Width = 550
        Me.Controls.Add(lblStatus)
    End Sub

    Private Sub SelectFile(sender As Object, e As EventArgs)
        Dim dialog As New OpenFileDialog()
        dialog.Filter = "PDF Files (*.pdf)|*.pdf"
        dialog.Title = "Select PDF File"

        If dialog.ShowDialog() = DialogResult.OK Then
            selectedFilePath = dialog.FileName
            txtFilePath.Text = selectedFilePath
        End If
    End Sub

    Private Async Sub UploadFileAsync(sender As Object, e As EventArgs)
        Try
            If String.IsNullOrEmpty(selectedFilePath) Then
                MessageBox.Show("Please select a PDF file first.")
                Return
            End If

            lblStatus.Text = "Uploading..."

            Dim url As String = "http://localhost:8000/upload_material"

            ' ' Add use_gpu as query parameter
            ' url &= "?use_gpu=" & chkUseGPU.Checked.ToString().ToLower()

            Using content As New MultipartFormDataContent()

                ' Read file bytes
                Dim fileBytes = File.ReadAllBytes(selectedFilePath)
                Dim fileContent As New ByteArrayContent(fileBytes)
                fileContent.Headers.ContentType =
                    New System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf")

                content.Add(fileContent, "file", Path.GetFileName(selectedFilePath))

                Dim response = Await httpClient.PostAsync(url, content)
                Dim responseText = Await response.Content.ReadAsStringAsync()

                If response.IsSuccessStatusCode Then

                    ' Parse JSON using System.Text.Json
                    Using doc As JsonDocument = JsonDocument.Parse(responseText)
                        Dim root = doc.RootElement

                        If root.TryGetProperty("status", Nothing) AndAlso
                           root.GetProperty("status").GetString() = "success" Then

                            Dim filename As String = root.GetProperty("filename").GetString()
                            lblStatus.Text = "Upload successful: " & filename
                            MessageBox.Show("Material processed successfully!")
                        Else
                            lblStatus.Text = "Upload failed."
                            MessageBox.Show("Error: " & responseText)
                        End If
                    End Using

                Else
                    lblStatus.Text = "Server error."
                    MessageBox.Show("Failed: " & response.StatusCode & vbCrLf & responseText)
                End If

            End Using

        Catch ex As Exception
            lblStatus.Text = "Error occurred."
            MessageBox.Show("Exception: " & ex.Message)
        End Try
    End Sub

End Class