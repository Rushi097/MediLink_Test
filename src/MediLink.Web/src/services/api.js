import axios from "axios";

export const apiUrl =
  import.meta.env.VITE_API_URL || "http://localhost:5140/api";

export const client = () =>
  axios.create({
    baseURL: apiUrl,
    headers: {
      Authorization: `Bearer ${localStorage.getItem("medilink-token")}`,
    },
  });

export const user = () =>
  JSON.parse(localStorage.getItem("medilink-user") || "null");
