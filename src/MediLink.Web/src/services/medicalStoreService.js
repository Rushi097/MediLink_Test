import axios from "axios";

const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5140/api";

export const getRegisteredStores = () => axios.get(`${apiUrl}/portal/stores`);

export const getStoreInventory = (storeId) =>
  axios.get(`${apiUrl}/portal/stores/${storeId}/inventory`);
