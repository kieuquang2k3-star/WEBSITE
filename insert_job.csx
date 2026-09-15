#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.Sqlite, 9.0.0"

using Microsoft.Data.Sqlite;

var db = @"d:\Claude Code\Mitech\src\Mitech.Web\mitech_dev.db";
using var conn = new SqliteConnection($"Data Source={db}");
conn.Open();

var cmd = conn.CreateCommand();
cmd.CommandText = """
INSERT INTO JobPositions (Slug, TitleVi, TitleJa, TitleEn, ShortDescVi, ShortDescJa, ShortDescEn, DetailVi, DetailJa, DetailEn, SalaryVi, SalaryJa, SalaryEn, SortOrder, IsActive)
VALUES (
  'kiem-tra-chat-luong-vis',
  'Kiểm tra chất lượng sản phẩm (VIS)',
  '品質検査（外観検査）',
  'Quality Inspector (Visual Inspection)',
  'Kiểm tra chất lượng sản phẩm bằng các thiết bị chuyên dụng, đo kiểm kích thước và đánh giá ngoại quan theo tiêu chuẩn.',
  '専用検査機器を使用した製品品質検査、寸法測定および外観評価を行います。',
  'Inspect product quality using specialized equipment, measure dimensions and evaluate appearance according to standards.',
  '<ul><li>Thực hiện kiểm tra chất lượng sản phẩm bằng kính phóng đại, camera, máy đo và các thiết bị kiểm tra chuyên dụng.</li><li>Đo kiểm kích thước, đánh giá ngoại quan sản phẩm theo tiêu chuẩn và ghi nhận kết quả vào báo cáo.</li><li>Được đào tạo nghiệp vụ từ đầu, ưu tiên ứng viên có kiến thức về chất lượng, kỹ thuật hoặc biết tiếng Nhật.</li></ul>',
  '<ul><li>ルーペ、カメラ、測定器および専用検査機器を使用して製品の品質検査を実施します。</li><li>寸法測定、外観検査を規格に基づいて行い、結果を報告書に記録します。</li><li>業務は最初から丁寧に教育します。品質・技術の知識または日本語能力がある方を優遇します。</li></ul>',
  '<ul><li>Perform product quality inspection using magnifying glasses, cameras, measuring instruments, and specialized inspection equipment.</li><li>Measure dimensions, evaluate product appearance according to standards, and record results in reports.</li><li>Full on-the-job training provided. Candidates with knowledge of quality control, engineering, or Japanese language skills are preferred.</li></ul>',
  '', '', '',
  3, 1
)
""";
var rows = cmd.ExecuteNonQuery();
Console.WriteLine($"Inserted {rows} row(s)");

// Verify
cmd.CommandText = "SELECT Id, Slug, TitleVi FROM JobPositions ORDER BY SortOrder";
using var reader = cmd.ExecuteReader();
while (reader.Read())
    Console.WriteLine($"{reader["Id"]}. {reader["Slug"]} — {reader["TitleVi"]}");
