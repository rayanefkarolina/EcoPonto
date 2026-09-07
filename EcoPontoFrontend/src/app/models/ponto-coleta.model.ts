import { Categoria } from './categoria.model';

export interface PontoColeta {
  id: number;
  nome: string;
  logradouro: string;
  bairro: string;
  cidade: string;
  telefone: string;
  horarioFuncionamento: string;
  latitude: number;
  longitude: number;
  categorias: Categoria[];
}
