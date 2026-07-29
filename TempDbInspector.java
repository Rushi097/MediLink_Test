import java.sql.*;

public class TempDbInspector {
    public static void main(String[] args) throws Exception {
        try (Connection connection = DriverManager.getConnection("jdbc:mysql://localhost:3306/MediLink?useSSL=false&serverTimezone=UTC", "root", "root")) {
            try (ResultSet rows = connection.createStatement().executeQuery("SELECT Email, Role, LENGTH(PasswordHash) FROM Users ORDER BY CreatedAt DESC LIMIT 20")) {
                while (rows.next()) System.out.println(rows.getString(1) + " | role=" + rows.getInt(2) + " | hashLength=" + rows.getInt(3));
            }
            for (String table : new String[] {"StoreOwnerProfiles", "Stores"}) {
                System.out.println("-- " + table + " --");
                try (ResultSet columns = connection.createStatement().executeQuery("SHOW COLUMNS FROM " + table)) {
                    while (columns.next()) System.out.println(columns.getString("Field") + " : " + columns.getString("Type"));
                }
                try (ResultSet rows = connection.createStatement().executeQuery("SELECT COUNT(*) FROM " + table)) { rows.next(); System.out.println("rows=" + rows.getInt(1)); }
            }
        }
    }
}
