import axios from 'axios';

const API_URL = 'https://localhost:5000/api/startups';

export const getStartups = () => axios.get(API_URL);
export const getStartupById = (id) => axios.get(API_URL + '/' + id);
export const createStartup = (startup) => axios.post(API_URL, startup);
export const updateStartup = (id, startup) => axios.put(API_URL + '/' + id, startup);
export const deleteStartup = (id) => axios.delete(API_URL + '/' + id);
